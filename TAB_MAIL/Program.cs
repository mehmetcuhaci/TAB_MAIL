using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using Serilog;

namespace TAB_MAIL
{
    internal class Program
    {
        private static DateTime lastRunDate;

        public static void Main(string[] args)
        {
            if (lastRunDate < SqlDateTime.MinValue.Value)
            {
                lastRunDate = SqlDateTime.MinValue.Value;
            }
            else if (lastRunDate > SqlDateTime.MaxValue.Value)
            {
                lastRunDate = SqlDateTime.MaxValue.Value;
            }

            DataTable customerData = GetCustomer();

            foreach (DataRow row in customerData.Rows)
            {
                string CustomerExtId = row["CUSTOMER_EXT_ID"].ToString();
                string fullName = row["FULL_NAME"].ToString();
                string mobilePhone = row["MOBILE_PHONE_NUMBER"].ToString();
                string email = row["EMAIL"].ToString();
                DateTime created = ConvertToDateTime(row["CREATED"].ToString());

                PostCustomer(CustomerExtId, fullName, mobilePhone, email, created);
            }

            string filePath = "lastRunDate.txt";

            if (File.Exists(filePath) && DateTime.TryParse(File.ReadAllText(filePath), out lastRunDate))
            {
                Console.WriteLine("Uygulama en son çalıştı: " + lastRunDate.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                Console.WriteLine("Bu uygulama daha önce hiç çalışmamış.");
                lastRunDate = DateTime.MinValue;
            }

            DateTime currentRunDate = DateTime.Now;
            File.WriteAllText(filePath, currentRunDate.ToString());
        }

        public static DateTime ConvertToDateTime(string createdString)
        {
            createdString = createdString.Replace("PM", " PM").Replace("AM", " AM"); // Önce AM ve PM arasındaki boşlukları ekleyin
            createdString = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(createdString.ToLower()); // Tüm harfleri küçük harfe çevirin ve başlık durumuna getirin
            createdString = createdString.Replace(" ", ""); // Boşlukları kaldırın

            var cultureInfo = new CultureInfo("en-US");
            var dateTimeFormat = "MMMddyyyyhttm";

            DateTime createdDateTime = DateTime.ParseExact(createdString, dateTimeFormat, cultureInfo);

            return createdDateTime;
        }


        public static DataTable GetCustomer()
        {
            using (var connection = new SqlConnection("user id=GTPDB;Password=GTPDB;data source=atagtp001;persist security info=False;Initial catalog=gtpbrdb"))
            {
                connection.Open();
                string searchQuery1 = "SELECT CUSTOMER_EXT_ID, FULL_NAME, MOBILE_PHONE_NUMBER, EMAIL, CREATED " +
                      "FROM GTPFSI_CUSTOMER_DETAILS_VIEW " +
                      "WHERE EMAIL LIKE '%@atayatirim.com.tr' " +
                      "AND CREATED > @lastRunDate " +
                      "ORDER BY CREATED DESC";

                SqlDataAdapter adapter = new SqlDataAdapter(searchQuery1, connection);
                adapter.SelectCommand.Parameters.AddWithValue("@lastRunDate", lastRunDate);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                connection.Close();

                return dataTable;
            }
        }

        public static void PostCustomer(string customerExtId, string fullName, string mobilePhone, string email, DateTime created)
        {
            HoldingLog();
            using (var connection = new SqlConnection("user id=sa;Password=ghibli117;data source=ata_web_db001;persist security info=False;Initial catalog=HospitalDatabase"))
            {
                connection.Open();

                string checkQuery = "SELECT COUNT(*) FROM TAB_GIDA_CUSTOMER WHERE CUSTOMER_EXT_ID = @customerExtId";

                using (var checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@customerExtId", customerExtId);
                    int count = (int)checkCmd.ExecuteScalar();
                    try
                    {
                        if (count > 0)
                        {
                            Console.WriteLine("Bu ID zaten mevcut, veri eklenmedi.");
                            Log.Error("Tekrar Var");
                        }
                        else
                        {
                            string insertQuery = "INSERT INTO TAB_GIDA_CUSTOMER (CUSTOMER_EXT_ID, FULL_NAME, MOBİLE_PHONE, EMAIL,CREATED) " +
                                                 "VALUES (@customerExtId, @fullName, @mobilePhone, @email,@created )";

                            using (var cmd = new SqlCommand(insertQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@customerExtId", customerExtId);
                                cmd.Parameters.AddWithValue("@fullName", fullName);
                                cmd.Parameters.AddWithValue("@mobilePhone", mobilePhone);
                                cmd.Parameters.AddWithValue("@email", email);
                                cmd.Parameters.AddWithValue("@created", created);

                                cmd.ExecuteNonQuery();

                                Console.WriteLine("Veri başarıyla eklendi.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Hata Var" + ex);
                    }
                    Log.CloseAndFlush();
                }

                connection.Close();
            }
        }

        public static void HoldingLog()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("C:\\MAIL_LOG\\Log.txt", rollingInterval: RollingInterval.Minute)
                .CreateLogger();
        }
    }
}
