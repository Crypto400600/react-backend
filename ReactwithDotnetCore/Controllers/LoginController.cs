using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using ReactwithDotnetCore.Model;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ReactwithDotnetCore.Controllers
{
    public class LoginController(IConfiguration configuration) : Controller
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string is missing.");

        [AllowAnonymous]
        [HttpPost("userlogin")]
        public IActionResult UserLogin([FromBody] User login)
        {
            IActionResult response = Unauthorized();
            var user = AuthenticateUser(login);
            if (user != null)
            {
                var tokenString = GenerateJSONWebToken(user);
                response = Ok(new { message = "success", token = tokenString });
            }
            return response;
        }

        [AllowAnonymous]
        [HttpPost("userregister")]
        public async Task<IActionResult> UserRegister([FromBody] User register)
        {
            try
            {
                using IDbConnection dbConnection = new SqlConnection(_connectionString);
                dbConnection.Open();

                string query = "INSERT INTO TBLB_User (username, emailaddress, password,dateofjoin) VALUES (@UserName, @EmailAddress, @Password , GETDATE())";
                int rowsAffected = await dbConnection.ExecuteAsync(query, register);

                if (rowsAffected > 0)
                {
                    return Ok(register);
                }
                else
                {
                    return BadRequest("Failed to insert the student record.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        private string GenerateJSONWebToken(User userInfo)
        {
            // Ensure the key has at least 256 bits
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration?["Jwt:Key"]?.PadRight(32)));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
                new Claim(JwtRegisteredClaimNames.Email, userInfo.EmailAddress),
                new Claim("DateOfJoin", userInfo.DateOfJoin.ToString("yyyy-MM-dd")),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(configuration?["Jwt:Issuer"],
                configuration?["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private User AuthenticateUser(User login)
        {
            using IDbConnection dbConnection = new SqlConnection(_connectionString);
            dbConnection.Open();

            // Example: Authenticate user based on Username and Password
            string query = "SELECT * FROM TBLB_User WITH(NOLOCK) WHERE Username = @Username AND Password = @Password";
            var users = dbConnection.Query<User>(query, new { login.Username, login.Password });

            // Assuming there should be only one matching user
            return users.FirstOrDefault();
        }
    }
}
