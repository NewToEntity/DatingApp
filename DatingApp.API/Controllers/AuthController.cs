using Microsoft.AspNetCore.Mvc;
using DatingApp.API.Data;
using System.Threading.Tasks;
using DatingApp.API.Models;
using DatingApp.API.Dtos;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel;


namespace DatingApp.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]  
      public class AuthController:ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly  IConfiguration _config;
        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            this._repo = repo;   
            this._config = config;   
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register( UserForRegisterDTO userForRegisterDTO )
        {
            // validate request

            userForRegisterDTO.Username = userForRegisterDTO.Username.ToLower();

            if ( await _repo.UserExists(userForRegisterDTO.Username))
                return BadRequest ("Username already exists");

            var userToCreate = new User
            {
                Username = userForRegisterDTO.Username
            };
            
            var createdUser = await _repo.Register(userToCreate, userForRegisterDTO.Password);

            return StatusCode(201);

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login( UserForLoginDTO userForLoginDTO )
        {
            // validate request

            var userFromRepo = await _repo.Login(userForLoginDTO.Username, userForLoginDTO.Password);

            if (userFromRepo is null)
                return Unauthorized();

//build the token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.Username)
            };

            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new {
                token = tokenHandler.WriteToken(token)
            });
        }

    }
}