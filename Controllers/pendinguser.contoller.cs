using Microsoft.AspNetCore.Mvc;
using TimeFourthe.Entities;
using TimeFourthe.Services;
using IdGenerator;
using TimeFourthe.Mails;
using System.Text.Json;
using System.Text;
using MongoDB.Bson;

namespace TimeFourthe.Controllers
{
    [Route("api")]
    [ApiController]
    public class PendingUsersContoller(PendingUserService pendingUserService, UserService userService) : ControllerBase
    {
        private readonly PendingUserService _pendingUserService = pendingUserService;
        private readonly UserService _userService = userService;

        [HttpPost("user/signup")]
        public async Task<IActionResult> CreatePendingUser([FromBody] User user)
        {
            var userExist = await _userService.GetUserAsync(user.Email);
            if (userExist != null) return Ok(new { error = true, message = "User already exists" });

            if (user.Role != "organization")
            {
                Console.WriteLine("Sign up for Teacher/Student");
                await _userService.CreateUserAsync(user);
                return Ok(new { message = "User created successfully", id = user.Id });
            }
            else
            {
                Console.WriteLine("Sign up for Organization");
                List<string> org = await _pendingUserService.CreatePendingUserAsync(user);
                Auth.Mail(org);
                return Ok(new { id = user.Name });
            }
        }


        // /get/auth?id={orgId}&answer=true
        [HttpGet("get/auth")]
        public async Task<IActionResult> GetAuth()
        {
            string orgId = Request.Query["id"].ToString();
            string approve = Request.Query["answer"].ToString();
            var deletedUser = await _pendingUserService.DeletePendingUserAsync(orgId);
            if (approve == "true")
            {
                HttpClient client = new HttpClient();
                string json = JsonSerializer.Serialize(deletedUser);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("http://localhost:3000/api/user/create", content);
                ApprovalSuccess.Mail(deletedUser.Name, deletedUser.Email);
                return Ok(new { message = "Your application is approved by autority", response });
            }
            else
            {
                return Ok(new { message = "Your application not approved by autority" });
            }
        }

    }
}