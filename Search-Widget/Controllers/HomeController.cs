using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Search_Widget.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Search_Widget.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var firstName = User.FindFirst("given_name")?.Value;
            ViewBag.FirstName = firstName; // Store the first name in ViewBag or use it as needed
            var isAdmin = User.IsInRole("Admin");
            var userClaims = User.Claims.ToList();
            // If you are specifically looking for the user's name claim
            var userNameClaim = userClaims.FirstOrDefault(c => c.Type == "name")?.Value;
            // Alternatively, you can display all claims if you're unsure about the claim types
            ViewData["UserClaims"] = userClaims;
            // Pass userNameClaim to view
            ViewData["UserName"] = userNameClaim;
            // Alternatively, get the user's roles directly from Claims
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            ViewData["UserRoles"] = userRoles;
            //HttpContext.Session.SetString("UserName", ViewData["UserName"]?.ToString());
            //string userName = HttpContext.Session.GetString("UserName");
            //Debug.WriteLine("Session UserName Home: " + userName);
            return View();
        }

        //public IActionResult Index()
        //{

        //    string connectionString = _configuration.GetConnectionString("DefaultConnection");

        //    using (var conn = new SqlConnection(connectionString))
        //    {
        //        conn.Open();
        //        Console.WriteLine("Connection Successful!");
        //    }


        //    var data = new List<Dictionary<string, object>>();
        //    //string connectionString = _configuration.GetConnectionString("DefaultConnection");

        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    using (SqlCommand cmd = new SqlCommand("SELECT TOP 100 * FROM vw_SearchWidgetCheckIn", conn))
        //    {
        //        try
        //        {
        //            //cmd.Parameters.AddWithValue("@OfficeID", OfficeID);
        //            conn.Open();
        //            var reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                //var row = Enumerable.Range(0, reader.FieldCount)
        //                //    .ToDictionary(reader.GetName, reader.GetValue);
        //                //data.Add(row);
        //                var row = new Dictionary<string, object>();

        //                for (int i = 0; i < reader.FieldCount; i++)
        //                {
        //                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
        //                    row.Add(reader.GetName(i), value);
        //                }
        //                data.Add(row);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("REAL ERROR: " + ex.ToString());
        //            throw;
        //        }
        //    }



        //    //var firstName = User.FindFirst("given_name")?.Value;
        //    //ViewBag.FirstName = firstName; // Store the first name in ViewBag or use it as needed
        //    //var isAdmin = User.IsInRole("Admin");
        //    var userClaims = User.Claims.ToList();
        //    //// If you are specifically looking for the user's name claim
        //    var userNameClaim = userClaims.FirstOrDefault(c => c.Type == "name")?.Value;
        //    var userEmailID = userClaims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        //    //// Alternatively, you can display all claims if you're unsure about the claim types
        //    //ViewData["UserClaims"] = userClaims;
        //    //// Pass userNameClaim to view
        //    ViewData["UserName"] = userNameClaim;
        //    ViewData["userEmailID"] = userEmailID;


        //    return View();
        //}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View();
        }


        [HttpGet]
        public IActionResult SearchProjects(
    string query,
    string type,
    bool filter,
    int page = 1,
    int pageSize = 10)
        {
            var results = new List<object>();

            string connectionString =
                _configuration.GetConnectionString("DefaultConnection");

            string sql;
            string countSql;

            int totalCount = 0;

            if (filter)
            {
                sql = @"SELECT Project, [Project Name], Client
                FROM dbo.vw_SearchWidgetCheckIn
                WHERE [Project Status] = 'A' AND ";

                countSql = @"SELECT COUNT(*)
                     FROM dbo.vw_SearchWidgetCheckIn
                     WHERE [Project Status] = 'A' AND ";
            }
            else
            {
                sql = @"SELECT Project, [Project Name], Client
                FROM dbo.vw_SearchWidgetCheckIn
                WHERE ";

                countSql = @"SELECT COUNT(*)
                     FROM dbo.vw_SearchWidgetCheckIn
                     WHERE ";
            }

            switch (type)
            {
                case "Project No":

                    //sql += "Project LIKE @query";                    
                    //countSql += "Project LIKE @query";
                    sql += "[Project] LIKE '%' + @query + '%'";
                    countSql += "[Project] LIKE '%' +  @query + '%'";
                    break;

                case "Project Name":

                    sql += "[Project Name] LIKE '%' + @query + '%'";
                    countSql += "[Project Name] LIKE '%' + @query + '%'";

                    break;

                case "Project Mgr":

                    sql += "[Project Manager] LIKE '%' + @query + '%'";
                    countSql += "[Project Manager] LIKE '%' + @query + '%'";

                    break;

                case "Client Name":

                    sql += "Client LIKE '%' + @query + '%'";
                    countSql += "Client LIKE '%' + @query + '%'";

                    break;

                default:

                    sql += @"(
                        Project LIKE '%' + @query + '%'
                        OR [Project Name] LIKE '%' + @query + '%'
                        OR Client LIKE '%' + @query + '%'
                        OR [Project Manager] LIKE '%' + @query + '%'
                     )";

                    countSql += @"(
                            Project LIKE '%' + @query + '%'
                            OR [Project Name] LIKE '%' + @query + '%'
                            OR Client LIKE '%' + @query + '%'
                            OR [Project Manager] LIKE '%' + @query + '%'
                          )";

                    break;
            }

            sql += @"
        ORDER BY Project DESC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY
        OPTION (RECOMPILE)";

            int offset = (page - 1) * pageSize;

            using (SqlConnection conn =
                new SqlConnection(connectionString))
            {
                conn.Open();

                // TOTAL COUNT
                using (SqlCommand countCmd =
                    new SqlCommand(countSql, conn))
                {
                    countCmd.CommandTimeout = 60;

                    countCmd.Parameters.Add(
                        "@query",
                        SqlDbType.VarChar
                    ).Value = query + "%";

                    totalCount =
                        Convert.ToInt32(countCmd.ExecuteScalar());
                }

                // PAGINATED DATA
                using (SqlCommand cmd =
                    new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 60;

                    cmd.Parameters.Add(
                        "@query",
                        SqlDbType.VarChar
                    ).Value = query + "%";

                    cmd.Parameters.Add(
                        "@Offset",
                        SqlDbType.Int
                    ).Value = offset;

                    cmd.Parameters.Add(
                        "@PageSize",
                        SqlDbType.Int
                    ).Value = pageSize;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new
                            {
                                client =
                                    reader["Client"]?.ToString(),

                                project =
                                    reader["Project"]?.ToString(),

                                projectName =
                                    reader["Project Name"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(new
            {
                data = results,
                totalCount = totalCount
            });
        }

        [HttpGet]
        public IActionResult GetProjectDetails(string id)
        {
            object result = null;

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(@"
       SELECT TOP 1 Project, [Project Name], Client, [Project Manager],[Project Manager Email],[Project Manager Status],[Office Practice Lead],[Office Practice Lead Status],[Office Practice Lead Email],[Project Status],[Vision_URL],ProjectWise_URL,[Salesforce_URL] FROM dbo.vw_SearchWidgetCheckIn WHERE Project = @ProjectId OPTION (RECOMPILE)", conn))
            {
                cmd.CommandTimeout = 60;
                cmd.Parameters.Add("@ProjectId", SqlDbType.VarChar).Value = id;
                //cmd.Parameters.Add("@ProjectId", SqlDbType.NVarChar).Value = id;

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = new
                        {
                            projectId = reader["Project"]?.ToString(),
                            clientName = reader["Client"]?.ToString(),
                            projectNumber = reader["Project"]?.ToString(),
                            projectName = reader["Project Name"]?.ToString(),
                            //status = "Active", // update if you have real column
                            status = reader["Project Status"]?.ToString(),
                            projectManager = reader["Project Manager"]?.ToString(),
                            projectManagerEmail = reader["Project Manager Email"]?.ToString(),
                            projectManagerStatus = reader["Project Manager Status"]?.ToString(),
                            officePracticeLead = reader["Office Practice Lead"]?.ToString(),
                            officePracticeLeadEmail = reader["Office Practice Lead Email"]?.ToString(),
                            officePracticeLeadStatus = reader["Office Practice Lead Status"]?.ToString(),
                            VisionUrl= reader["Vision_URL"]?.ToString(),
                            ProjectWiseUrl = reader["ProjectWise_URL"]?.ToString(),
                            SalesforceUrl = reader["Salesforce_URL"]?.ToString()
                            //practiceLeader = reader["PracticeLeader"]?.ToString(),
                            //org = reader["CustTRCOrg"]?.ToString()
                        };
                    }
                }
            }

            return Json(result);
        }
    }
}
