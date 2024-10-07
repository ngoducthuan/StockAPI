using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Dapper;
using StockAPI.DTOs;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace StockAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnection _connection;

        public AuthenticationController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        [HttpPut("purchase/{userId}")]
        public async Task<IActionResult> UpdateUserFinance(int userId)
        {
            // Update remaining_balance and money_spent
            string sql = "UPDATE user_finances " +
                         "SET remaining_balance = remaining_balance - 10000, " +
                         "    money_spent = money_spent + 10000 " +
                         "WHERE user_id = @userId";

            try
            {
                await _connection.OpenAsync(); // Open connection

                using (var command = new SqlCommand(sql, _connection))
                {
                    // Add parameter
                    command.Parameters.AddWithValue("@userId", userId);

                    // Execute query
                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    // Check record
                    if (rowsAffected > 0)
                    {
                        //Get usser info
                        string userQuery = @"
                            SELECT 
                                u.id AS user_id,
                                u.firstname,
                                u.lastname,
                                u.email,
                                u.phone,
                                u.birth,
                                u.organization,
                                u.location,
                                l.username,
                                l.password,
                                uf.remaining_balance,
                                uf.money_spent
                            FROM 
                                users u
                            LEFT JOIN 
                                logins l ON u.id = l.user_id
                            LEFT JOIN 
                                user_finances uf ON u.id = uf.user_id
                            WHERE 
                                u.id = @userId";
                        var user = await _connection.QueryFirstOrDefaultAsync<DTOs.UserInfo>(userQuery, new { userId = userId });

                        return Ok(user);
                    }
                    return NotFound(new { message = "Không tìm thấy người dùng với userId đã cung cấp" });
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, "Lỗi khi thực hiện truy vấn: " + ex.Message);
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    await _connection.CloseAsync(); // Close connection
                }
            }
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> DepositUserFinance([FromBody] DTOs.PaymentDTO payment)
        {
            // Update remaining_balance and money_spent
            string sql = "UPDATE user_finances " +
                         "SET remaining_balance = remaining_balance + @depositMoney " +
                         "WHERE user_id = @userId";

            try
            {
                await _connection.OpenAsync(); // Open connection

                using (var command = new SqlCommand(sql, _connection))
                {
                    // Add parameter
                    command.Parameters.AddWithValue("@userId", payment.userId);
                    command.Parameters.AddWithValue("@depositMoney", payment.depositMoney);

                    // Execute query
                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    // Check record
                    if (rowsAffected > 0)
                    {
                        //Get usser info
                        string userQuery = @"
                            SELECT 
                                u.id AS user_id,
                                u.firstname,
                                u.lastname,
                                u.email,
                                u.phone,
                                u.birth,
                                u.organization,
                                u.location,
                                l.username,
                                l.password,
                                uf.remaining_balance,
                                uf.money_spent
                            FROM 
                                users u
                            LEFT JOIN 
                                logins l ON u.id = l.user_id
                            LEFT JOIN 
                                user_finances uf ON u.id = uf.user_id
                            WHERE 
                                u.id = @userId";
                        var user = await _connection.QueryFirstOrDefaultAsync<DTOs.UserInfo>(userQuery, new { userId = payment.userId });

                        return Ok(user);
                    }
                    return NotFound(new { message = "Không tìm thấy người dùng với userId đã cung cấp" });
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, "Lỗi khi thực hiện truy vấn: " + ex.Message);
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                {
                    await _connection.CloseAsync(); // Close connection
                }
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DTOs.UserLoginDTO loginDto)
        {
            string query = "SELECT * FROM logins WHERE username = @username AND password = @password";
            var login = await _connection.QueryFirstOrDefaultAsync<DTOs.User>(query, new { loginDto.Username, loginDto.Password });
            if (login == null)
            {
                return Unauthorized("Invalid username or password");
            }

            // Get all user info
            string userQuery = @"
                            SELECT 
                                u.id AS user_id,
                                u.firstname,
                                u.lastname,
                                u.email,
                                u.phone,
                                u.birth,
                                u.organization,
                                u.location,
                                l.username,
                                l.password,
                                uf.remaining_balance,
                                uf.money_spent
                            FROM 
                                users u
                            LEFT JOIN 
                                logins l ON u.id = l.user_id
                            LEFT JOIN 
                                user_finances uf ON u.id = uf.user_id
                            WHERE 
                                u.id = @userId";
            var user = await _connection.QueryFirstOrDefaultAsync<DTOs.UserInfo>(userQuery, new { userId = login.user_id });

            //return Ok(user);
            /// Tạo JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.user_id.ToString()),
                    new Claim(ClaimTypes.Name, user.username),
                    new Claim(ClaimTypes.Email, user.email),
                    // new Claim(ClaimTypes.Role, user.role) // Thêm vai trò vào token nếu cần
                }),
                Expires = DateTime.UtcNow.AddMinutes(5), // Thời gian tồn tại của token
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            Console.WriteLine("Token Expires At: " + tokenDescriptor.Expires.ToString());

            return Ok(new
            {
                Token = tokenString,
                User = user
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO registerDto)
        {
            // Kiểm tra xem email đã tồn tại chưa
            string checkUserQuery = "SELECT COUNT(1) FROM users WHERE email = @Email";
            var userExists = await _connection.ExecuteScalarAsync<int>(checkUserQuery, new { registerDto.Email });

            if (userExists > 0)
            {
                return BadRequest("Email already exists.");
            }

            // Kiểm tra xem username đã tồn tại chưa
            string checkUsernameQuery = "SELECT COUNT(1) FROM logins WHERE username = @Username";
            var usernameExists = await _connection.ExecuteScalarAsync<int>(checkUsernameQuery, new { registerDto.Username });

            if (usernameExists > 0)
            {
                return BadRequest("Username already exists.");
            }

            // Tạo người dùng mới
            string insertUserQuery = @"
            INSERT INTO users (firstname, lastname, email, phone, birth, organization, location)
            VALUES (@FirstName, @LastName, @Email, @Phone, @Birth, @Organization, @Location);
            SELECT CAST(SCOPE_IDENTITY() as int);"; // Lấy ID của người dùng mới

            var userId = await _connection.ExecuteScalarAsync<int>(insertUserQuery, registerDto);

            // Tạo đăng nhập mới cho người dùng
            string insertLoginQuery = @"
            INSERT INTO logins (user_id, username, password)
            VALUES (@UserId, @Username, @Password);";

            // Lưu password dưới dạng hash (khuyến nghị)
            //var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            await _connection.ExecuteAsync(insertLoginQuery, new
            {
                UserId = userId,
                Username = registerDto.Username,
                Password = registerDto.Password
            });

            // Chèn vào bảng user_finances với remaining_balance và money_spent mặc định là 0
            string insertFinanceQuery = @"
            INSERT INTO user_finances (user_id, remaining_balance, money_spent)
            VALUES (@UserId, 0, 0);"; // Chèn mặc định là 0

            await _connection.ExecuteAsync(insertFinanceQuery, new
            {
                UserId = userId
            });

            return Ok(new { message = "User registered successfully." });
        }

    }
}
