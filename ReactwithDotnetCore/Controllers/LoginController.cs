using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using ReactwithDotnetCore.Model;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ReactwithDotnetCore.Controllers
{
    public class LoginController(IConfiguration configuration) : Controller
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string is missing.");

        [AllowAnonymous]
        [HttpPost("userlogin")]
        public async Task<IActionResult> UserLogin([FromBody] User login)
        {
            IActionResult response = Unauthorized();
            var user = await AuthenticateUser(login);
            if (user != null)
            {
                var (tokenString, refreshToken) = GenerateTokens(user);
                response = Ok(new { message = "success", token = tokenString, refreshToken });
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

        /// <summary>
        /*
         * 
        The purpose of a refresh token is to provide a way to obtain a new access token without requiring the
        user to re-enter their credentials.Access tokens have a limited lifespan, and when they expire, the 
        user would typically need to log in again to get a new access token.

        With a refresh token mechanism, when the access token expires, the client can use the refresh token 
        to obtain a new access token without requiring the user's credentials. This helps in maintaining a 
        balance between security and user convenience. The refresh token is a long-lived token that can 
        be securely stored by the client and used to request new access tokens as needed.
        *
        */
        /// </summary>
        /// <param name="refreshTokenRequest"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("refreshtoken")]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest refreshTokenRequest)
        {
            IActionResult response = BadRequest("Invalid token");
            var principal = GetPrincipalFromExpiredToken(refreshTokenRequest.Token);

            if (principal != null)
            {
                var username = principal?.Claims?.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                if (username != null)
                {
                    var user = GetUserByUsername(username);

                    if (user != null && refreshTokenRequest.RefreshToken == user.RefreshToken)
                    {
                        var (tokenString, newRefreshToken) = GenerateTokens(user);
                        response = Ok(new { token = tokenString, refreshToken = newRefreshToken });
                    }
                }
            }

            return response;
        }

        private (string tokenString, string refreshToken) GenerateTokens(User userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]?.PadRight(32) ?? string.Empty));
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

            var refreshToken = GenerateRefreshToken();
            userInfo.RefreshToken = refreshToken; // Save refresh token to user in your data store

            // Update the refresh token in the database
            UpdateRefreshTokenInDatabase(userInfo.Username, refreshToken);

            return (new JwtSecurityTokenHandler().WriteToken(token), refreshToken);
        }

        private static string GenerateRefreshToken()
        {
            // Generate a random refresh token (you may use a more sophisticated method)
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // This will allow an expired token to be parsed
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty))
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            // The following line will throw an exception if the token is expired
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            return principal;
        }

        private async Task<User> AuthenticateUser(User login)
        {
            using IDbConnection dbConnection = new SqlConnection(_connectionString);
            dbConnection.Open();

            string query = "SELECT * FROM TBLB_User WITH(NOLOCK) WHERE Username = @Username AND Password = @Password";
            var users = await dbConnection.QueryAsync<User>(query, new { login.Username, login.Password });

            return users.FirstOrDefault() ?? new User();
        }

        private User GetUserByUsername(string username)
        {
            using IDbConnection dbConnection = new SqlConnection(_connectionString);
            dbConnection.Open();

            string query = "SELECT * FROM TBLB_User WITH(NOLOCK) WHERE Username = @Username";
            var user = dbConnection.Query<User>(query, new { Username = username }).FirstOrDefault();

            return user ?? new User();
        }

        private void UpdateRefreshTokenInDatabase(string username, string newRefreshToken)
        {
            using IDbConnection dbConnection = new SqlConnection(_connectionString);
            dbConnection.Open();

            string updateQuery = "UPDATE TBLB_User SET RefreshToken = @RefreshToken WHERE Username = @Username";
            dbConnection.Execute(updateQuery, new { RefreshToken = newRefreshToken, Username = username });
        }
    }
}
