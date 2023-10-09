using System;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using Serilog;

namespace TAB_MAIL
{
    internal class Program
    {
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
            using (var connection = new SqlConnection("user id=GTPDB;Password=GTPDB;data source=atagtp001;persist security info=False;Initial catalog=gtpbrdb"))
            {
                connection.Open();
                string searchQuery1 = "SELECT CUSTOMER_EXT_ID, FULL_NAME, MOBILE_PHONE_NUMBER, EMAIL, CREATED FROM GTPFSI_CUSTOMER_DETAILS_VIEW WHERE CREATED >= '2023-10-01' AND EMAIL LIKE '%@atayatirim.com.tr'";

                SqlDataAdapter adapter = new SqlDataAdapter(searchQuery1, connection);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                connection.Close();

                if (dataTable.Rows.Count == 0)
                {
                    Console.WriteLine("Kullanıcı bulunamamıştır");
                    Console.ReadLine();
                }

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

                               // E - posta gönderme
                               // SmtMail(email);        ABİ BURAYI AÇIP ÇALIŞTIRMA HEPSİNE MAİL ATAR :D
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


        public static void SmtMail(string toEmail)
        {
            HoldingLog();
            string smtpServer = "smtp-mail.outlook.com";
            int smtpPort = 587;
            string smtpUsername = "mehmet17014@hotmail.com";
            string smtpPassword = "021302135i";

            using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = true;

                MailMessage mail = new MailMessage(smtpUsername, toEmail);
                mail.Subject = "TEST";
                mail.Body = "TEST E POSTASI";

                try
                {
                    smtpClient.Send(mail);
                    Console.WriteLine("Gönderildi");
                    Log.Information("Mail başarıyla gönderildi!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Gönderilemedi Hata : {ex.Message}");
                    Log.Error($"HATA: {ex.Message}");
                }
            }
            Log.CloseAndFlush();
        }

        public static void HoldingLog()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("C:\\MAIL_LOG\\Log.txt", rollingInterval: RollingInterval.Minute)
                .CreateLogger();
        }
    }
}
