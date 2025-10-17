using System;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace VulnerableApp.Controllers
{
    [Route("api/[controller]")]
    public class VulnerableController : Controller
    {
        private readonly string _connectionString = "Server=myServer;Database=myDB;User Id=myUser;Password=myPass;";
        private static readonly HttpClient _httpClient = new HttpClient();

       
        [HttpGet("/get-user")]
        public IActionResult GetUser(string username)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Users WHERE Username = @username";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new { message = "User found" });
                        }
                    }
                }
            }
            return NotFound(new { message = "User not found" });
        }

        
        [HttpGet("makerequestf")]
        public async Task<IActionResult> FetchUrl(string targetUrl)
        {
            var response = await _httpClient.GetStringAsync(targetUrl);
            return Ok(response);
        }

       
        [HttpGet("user-data/{userId}")]
        public IActionResult GetUserData(int userId)
        {
            int currentUserId = int.Parse(User.Identity.Name);
            if (userId == currentUserId || User.IsInRole("Admin"))
            {
                return Ok(new { message = "Here is the user data" });
            }
            return Unauthorized();
        }

       
        [HttpPost("login")]
        public IActionResult Login(string username, string password)
        {
            bool userExists = CheckUserExists(username);
            if (!userExists)
            {
                return BadRequest(new { message = "Invalid username" }); 
            }
            if (!ValidatePassword(username, password))
            {
                return BadRequest(new { message = "Invalid credentials" });
            }
            return Ok(new { message = "Login successful" });
        }

        private bool CheckUserExists(string username)
        {

            return username == "admin" || username == "user";
        }

        private bool ValidatePassword(string username, string password)
        {
            return password == "password123"; 
        }

        [HttpGet("get-content")]
        public ContentResult GetContent(string input)
        {
            return Content("<html><body>" + input + "</body></html>", "text/html"); 
        }
        
        [HttpPost("update-user")]
        public IActionResult UpdateUser(string email, string phone)
        {
            if (email.Contains("@") && phone.Length > 5)
            {
                return Ok(new { message = "User updated" });
            }
            if (!phone.All(char.IsDigit))
            {
                return BadRequest(new { message = "Invalid phone number" });
            }
            return Ok(new { message = "User updated with issues" });
        }
    }
}
