using System.Threading.Tasks;
using System.Linq;
using DatingApp.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DatingApp.API.Dtos;
using Microsoft.AspNetCore.Identity;
using DatingApp.API.Models;
using AutoMapper;
using System.Collections.Generic;
using System.Security.Claims;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.Extensions.Options;
using DatingApp.API.Helpers;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;
        public AdminController(DataContext context, UserManager<User> userManager, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _mapper = mapper;
            _userManager = userManager;
            _context = context;
            _cloudinaryConfig = cloudinaryConfig;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("usersWithRoles")]
        public async Task<IActionResult> GetUsersWithRoles()
        {
            var userList = await (
                from user in _context.Users
                orderby user.UserName
                select new
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Roles = (from userRole in user.UserRoles
                             join role in _context.Roles
                             on userRole.RoleId
                             equals role.Id
                             select role.Name).ToList()
                }).ToListAsync();

            return Ok(userList);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("editRoles/{userName}")]
        public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEditDto)
        {
            var user = await _userManager.FindByNameAsync(userName);

            var userRoles = await _userManager.GetRolesAsync(user);

            var selectedRoles = roleEditDto.RoleNames;

            selectedRoles = selectedRoles ?? new string[] { };
            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if (!result.Succeeded)
                return BadRequest("Failed to add to roles");

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!result.Succeeded)
                return BadRequest("Failed to remove the roles");

            return Ok(await _userManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photosForModeration")]
        public async Task<IActionResult> GetPhotosForModeration()
        {
            var photos = await _context.Photos.Where(p => !p.IsApproved).Include(u => u.User).IgnoreQueryFilters().ToListAsync();

            var photosToReturn = _mapper.Map<IEnumerable<PhotoForReturnDto>>(photos);

            return Ok(photosToReturn);
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApprovePhoto(int id)
        {
            var photo = await _context.Photos.Where(p => p.Id == id).IgnoreQueryFilters().FirstOrDefaultAsync();
            photo.IsApproved = true;
            
            if (await _context.SaveChangesAsync() > 0)
            {
                return NoContent();
            }

            return BadRequest("Could not approve photo");
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpDelete("reject/{userId}/{id}")]
        public async Task<IActionResult> RejectPhoto(int userId, int id)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Include(u => u.Photos)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync();

            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();
            
            var photoFromRepo = await _context.Photos.IgnoreQueryFilters().Where(p => p.Id == id).FirstOrDefaultAsync();

            if (photoFromRepo.IsMain)
                return BadRequest("You cannot delete your main photo");

            if (photoFromRepo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);

                var result = _cloudinary.Destroy(deleteParams);

                if (result.Result == "ok")
                {
                    _context.Remove(photoFromRepo);
                }
            }

            if (photoFromRepo.PublicId == null)
            {
                _context.Remove(photoFromRepo);
            }

            if (await _context.SaveChangesAsync() > 0)
                return Ok();
            
            return BadRequest("Failed to delete the photo");
        }
    }
}