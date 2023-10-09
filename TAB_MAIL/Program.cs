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
            DataTable customerData = GetCustomer();

            foreach (DataRow row in customerData.Rows)
            {
                string CustomerExtId = row["CUSTOMER_EXT_ID"].ToString();
                string fullName = row["FULL_NAME"].ToString();
                string mobilePhone = row["MOBILE_PHONE_NUMBER"].ToString();
                string email = row["EMAIL"].ToString();
                DateTime created = Convert.ToDateTime(row["CREATED"]);

                PostCustomer(CustomerExtId, fullName, mobilePhone, email, created);
            }
            
        }

        public static DataTable GetCustomer()
        {
            DateTime currentTime = new DateTime(2023, 10, 01);

            using (var connection = new SqlConnection("user id=GTPDB;Password=GTPDB;data source=atagtp001;persist security info=False;Initial catalog=gtpbrdb"))
            {
                connection.Open();
                string searchQuery1 = "SELECT CUSTOMER_EXT_ID, FULL_NAME, MOBILE_PHONE_NUMBER, EMAIL, CREATED " +
                      "FROM GTPFSI_CUSTOMER_DETAILS_VIEW " +
                      "WHERE CREATED >= @currentTime AND EMAIL LIKE '%@atayatirim.com.tr' " +
                      "ORDER BY CREATED DESC";


                SqlCommand command = new SqlCommand(searchQuery1, connection);
                command.Parameters.AddWithValue("@currentTime", DateTime.Now);
                SqlDataAdapter adapter = new SqlDataAdapter(searchQuery1, connection);
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
