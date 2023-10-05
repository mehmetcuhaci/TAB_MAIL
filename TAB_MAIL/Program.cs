using System;
using System.Data;
using System.Data.SqlClient;

namespace TAB_MAIL
{
    internal class Program
    {
        public static void Main(string[] args)
        { 
             DataTable customerData = GetCustomer(); // GetCustomer'dan verileri al
            foreach (DataRow row in customerData.Rows)
            {
                string CustomerExtId = row["CUSTOMER_EXT_ID"].ToString();
                string fullName = row["FULL_NAME"].ToString();
                string mobilePhone = row["MOBILE_PHONE_NUMBER"].ToString();
                string email = row["EMAIL"].ToString();
                DateTime created = Convert.ToDateTime(row["CREATED"]);

                PostCustomer(CustomerExtId, fullName, mobilePhone, email, created); // Verileri PostCustomer ile gönder
            }
        }

        public static DataTable GetCustomer()
        {
            using (var connection = new SqlConnection("user id=GTPDB;Password=GTPDB;data source=atagtp001;persist security info=False;Initial catalog=gtpbrdb"))
            {
                connection.Open();
                string searchQuery1 = "SELECT CUSTOMER_EXT_ID, FULL_NAME, MOBILE_PHONE_NUMBER, EMAIL, CREATED FROM GTPFSI_CUSTOMER_DETAILS_VIEW WHERE EMAIL LIKE '%@atayatirim.com.tr' ORDER BY CREATED DESC";

                SqlDataAdapter adapter = new SqlDataAdapter(searchQuery1, connection);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                connection.Close();

                return dataTable;
            }
        }

        public static void PostCustomer(string customerExtId, string fullName, string mobilePhone, string email, DateTime created)
        {
            using (var connection = new SqlConnection("user id=sa;Password=ghibli117;data source=ata_web_db001;persist security info=False;Initial catalog=HospitalDatabase"))
            {
                connection.Open();

                // Önce veritabanında aynı ID var mı diye kontrol et
                string checkQuery = "SELECT COUNT(*) FROM TAB_GIDA_CUSTOMER WHERE CUSTOMER_EXT_ID = @customerExtId";

                using (var checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@customerExtId", customerExtId);
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        Console.WriteLine("Bu ID zaten mevcut, veri eklenmedi.");
                    }
                    else
                    {
                        // ID mevcut değilse ekleme işlemine devam et
                        string insertQuery = "INSERT INTO TAB_GIDA_CUSTOMER (CUSTOMER_EXT_ID, FULL_NAME, MOBİLE_PHONE, EMAIL, CREATED) " +
                                             "VALUES (@customerExtId, @fullName, @mobilePhone, @email, @created)";

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

                connection.Close();
            }
        }

        /*SELECT CUSTOMER_EXT_ID,EMAIL
          FROM TAB_GIDA_CUSTOMER
          GROUP BY CUSTOMER_EXT_ID,EMAIL
          HAVING COUNT(DISTINCT EMAIL)> 0; */


    }
}
