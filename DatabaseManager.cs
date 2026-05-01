using MySql.Data.MySqlClient;

namespace CardGameServer
{
    public class DatabaseManager
    {
        public static DatabaseManager Instance { get; } = new DatabaseManager();

        private string _connectionString;

        private DatabaseManager() { }

        public void Init(string host, string database, string user, string password)
        {
            _connectionString =
                $"Server={host};Database={database};User ID={user};Password={password};";
            try
            {
                using var conn = GetConnection();
                Console.WriteLine("[DB] 数据库连接成功！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] 数据库连接失败: {ex.Message}");
            }
        }

        private MySqlConnection GetConnection()
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        // 注册
        public bool Register(string userName, string email, string passwordHash)
        {
            try
            {
                using var conn = GetConnection();
                // 检查邮箱是否已注册
                using var checkCmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM user WHERE email = @email", conn);
                checkCmd.Parameters.AddWithValue("@email", email);
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    Console.WriteLine("[DB] 邮箱已注册");
                    return false;
                }

                using var cmd = new MySqlCommand(
                    "INSERT INTO user (user_name, email, password) VALUES (@name, @email, @pwd)",
                    conn);
                cmd.Parameters.AddWithValue("@name", userName);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@pwd", passwordHash);
                cmd.ExecuteNonQuery();
                Console.WriteLine($"[DB] 注册成功: {userName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] 注册失败: {ex.Message}");
                return false;
            }
        }

        // 登录验证，成功返回玩家ID，失败返回 -1
        public UseInfo Login(string email, string passwordHash)
        {
            try
            {
                using var conn = GetConnection();
                using var cmd = new MySqlCommand(
                    "SELECT id, user_name FROM user WHERE email = @email AND password = @pwd",
                    conn);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@pwd", passwordHash);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    long id = reader.GetInt64("id");
                    string name = reader.GetString("user_name");
                    Console.WriteLine($"[DB] 登录成功: {name}, ID: {id}");
                    return new UseInfo { userId = id, userName = name };
                }
                Console.WriteLine("[DB] 登录失败：邮箱或密码错误");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] 登录异常: {ex.Message}");
                return null;
            }
        }
    }
}