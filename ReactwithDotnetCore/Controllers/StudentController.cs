using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ReactwithDotnetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]

    public class StudentController(IConfiguration configuration) : ControllerBase
    {
        private readonly string? _connectionString = configuration.GetConnectionString("DefaultConnection");
        private readonly string? _connectionString2 = configuration.GetConnectionString("DefaultConnection2");

        /// <summary>
        /// Student Data Process API
        /// </summary>
        public class Student
        {
            public int? rollNumber { get; set; }
            public string? name { get; set; }
            public string? email { get; set; }
            public string? phone { get; set; }
            public string? image { get; set; }
        }

        [HttpPost("studentdatapost")]
        public async Task<IActionResult> StudentData2(Student student)
        {
            try
            {
                using IDbConnection dbConnection = new SqlConnection(_connectionString);
                dbConnection.Open();

                string query = "INSERT INTO TBLB_Student (name, email, phone, image) VALUES (@Name, @Email, @Phone, @Image)";
                int rowsAffected = await dbConnection.ExecuteAsync(query, student);

                if (rowsAffected > 0)
                {
                    return Ok(student);
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

        [Authorize]
        [HttpGet("getallstudents")]
        public async Task<IActionResult> GetAllStudents()
        {
            try
            {
                /*
                
                //This code assumes that the token is in the "Bearer <token>" format in the Authorization header. 
                //It splits the header and takes the last part as the token for validation. If your token format is 
                //different, adjust the code accordingly.

                // Retrieve the user name from the claims
                var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Retrieve the token from the Authorization header
                var token = HttpContext.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();

                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized("Token not provided");
                }

                // Validate the token
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration?["Jwt:Issuer"]?.ToString(),
                    ValidAudience = configuration?["Jwt:Audience"]?.ToString(),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration?["Jwt:Key"]?.PadRight(32)))
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // At this point, the token is valid, and you can retrieve additional claims
                var dateOfJoin = principal.FindFirst("DateOfJoin")?.Value;

                */

                using IDbConnection dbConnection = new SqlConnection(_connectionString);
                dbConnection.Open();

                string query = "SELECT * FROM TBLB_Student";
                var students = await dbConnection.QueryAsync<Student>(query);

                return Ok(students);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPost("insertstudent")]
        public async Task<IActionResult> InsertStudent(Student student)
        {
            try
            {
                using IDbConnection dbConnection = new SqlConnection(_connectionString);
                dbConnection.Open();

                if (student.rollNumber.HasValue && student.rollNumber > 0)
                {
                    // Update record if rollNumber is greater than 0
                    string updateQuery = "UPDATE TBLB_Student SET name = @Name, email = @Email, phone = @Phone, image = @Image WHERE rollNumber = @rollNumber;";
                    int rowsAffected = await dbConnection.ExecuteAsync(updateQuery, student);

                    if (rowsAffected > 0)
                    {
                        string query = "SELECT * FROM TBLB_Student";
                        var students = await dbConnection.QueryAsync<Student>(query);

                        return Ok(students);
                    }
                    else
                    {
                        return BadRequest("Failed to update the student record.");
                    }
                }
                else
                {
                    // Insert record if rollNumber is not provided or less than or equal to 0
                    string insertQuery = "INSERT INTO TBLB_Student (name, email, phone, image) VALUES (@Name, @Email, @Phone, @Image);";
                    int rowsAffected = await dbConnection.ExecuteAsync(insertQuery, student);

                    if (rowsAffected > 0)
                    {
                        string query = "SELECT * FROM TBLB_Student";
                        var students = await dbConnection.QueryAsync<Student>(query);

                        return Ok(students);
                    }
                    else
                    {
                        return BadRequest("Failed to insert the student record.");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPost("deletestudent/{rollNumber}")]
        public async Task<IActionResult> DeleteStudent(int rollNumber)
        {
            try
            {
                using IDbConnection dbConnection = new SqlConnection(_connectionString);
                dbConnection.Open();

                string query = "DELETE FROM TBLB_Student WHERE rollNumber = @rollNumber";
                int rowsAffected = await dbConnection.ExecuteAsync(query, new { RollNumber = rollNumber });

                if (rowsAffected > 0)
                {
                    return Ok($"Student with Roll Number {rollNumber} deleted successfully.");
                }
                else
                {
                    return NotFound($"Student with Roll Number {rollNumber} not found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sale Data TEST API
        /// </summary>

        [HttpGet("storenodata")]
        public async Task<IActionResult> GetStoreNo()
        {
            using var connection = new SqlConnection(_connectionString2);
            connection.Open();
            var sales = await connection.QueryAsync<dynamic>(@"SELECT DISTINCT
                  StoreID
                FROM TBLT_Sale WITH(NOLOCK) 
                ORDER BY StoreID DESC;
            ");
            return Ok(sales);
        }

        [HttpGet("salesdata")]
        public async Task<IActionResult> GetSales()
        {
            using var connection = new SqlConnection(_connectionString2);
            connection.Open();
            var sales = await connection.QueryAsync<dynamic>(@"SELECT SaleID
                  ,UniqueSessionId
                  ,StoreID
                  ,SaleQty
                  ,SaleTotal
                  ,SaleUserName
                FROM TBLT_Sale WITH(NOLOCK) 
                ORDER BY SaleID DESC;
            ");
            return Ok(sales);
        }

        [HttpGet("saleslogdata")]
        public async Task<IActionResult> GetSalesLogs()
        {
            using var connection = new SqlConnection(_connectionString2);
            connection.Open();
            var saleslogs = await connection.QueryAsync<dynamic>(@"SELECT LogId
                  ,LogType
                  ,CSVFileName
                  ,SendTime
                  ,ResponseTime
                  ,Status
                FROM TBLB_SalesSendDataLog WITH(NOLOCK)
                ORDER BY LogId DESC;
            ");
            return Ok(saleslogs);
        }

        [HttpPost("salesdatapost")]
        public IActionResult SendSalesData(List<dynamic> objsales)
        {
            return Ok(objsales);
        }

        [HttpGet("dummy")]
        public IActionResult GetDummyData()
        {
            var dummyData = new List<string> { "Dummy1", "Dummy2", "Dummy3" };
            return Ok(dummyData);
        }
    }
}
