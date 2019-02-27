using System.Threading.Tasks;
using DatingApp.API.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers 
{
    [Authorize]
    [Route ("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase 
    {
        private readonly IDatingRepository _repository;
        public UsersController (IDatingRepository repository) 
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers() 
        {
            var users = await _repository.GetUsers();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _repository.GetUser(id);

            return Ok(user);
        }
    }
}