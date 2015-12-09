using System.Globalization;
using System.ServiceModel.Channels;

namespace TimelogDataMiner
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using System.Security.Policy;
    using System.ServiceModel;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using System.Xml;
    using System.Xml.Linq;

    using TimelogDataMiner.TimelogSecurity;

    class Program
    {
        #region Fields

        private static TimelogProjectManagement.ProjectManagementServiceClient projectManagementClient;
        private static TimelogSecurity.SecurityServiceClient securityClient;
        private static TimelogServiceReference.ServiceSoapClient serviceClient;

        #endregion Fields

        #region Properties

        public static TimelogServiceReference.ServiceSoapClient ServiceClient
        {
            get
            {
                if (serviceClient == null)
                {
                    var binding = new BasicHttpsBinding() { MaxReceivedMessageSize = Int32.MaxValue }; //1024000
                    var endpoint = new EndpointAddress(Config.Url + "/service.asmx");
                    serviceClient = new TimelogServiceReference.ServiceSoapClient(binding, endpoint);
                }

                return serviceClient;
            }
        }

        #endregion Properties

        #region Methods

        static void Main(string[] args)
        {
            Console.WriteLine(string.Format("Executing TimeLog DataMiner..."));
            var startTimer = DateTime.Now;

            try
            {
                // Loading Base Information
                GetCustomer();
                GetProject();
                GetTask();

                // Loading Base Employee list
                #region GetEmployeesRaw
                XmlNode result = ServiceClient.GetEmployeesRaw(Config.SiteCode, Config.ApiID, Config.ApiPassword, 0, string.Empty, 0, -1);
                File.CreateText("TimeLog.Employees.xml").Write(result.OuterXml);
            
                if (!result.HasChildNodes)
                {
                    throw new Exception("No results found on GetEmployeesRaw - exiting to console");
                    return;
                }

                XDocument xdoc = XDocument.Parse(result.OuterXml);
                XNamespace ns = "http://www.timelog.com/XML/Schema/tlp/v4_4";
                var employees = from u in xdoc.Descendants(ns + "Employee")
                                select new
                                {
                                    Id = (int)u.Attribute("ID"),
                                    FirstName = (string)u.Element(ns + "FirstName"),
                                    LastName = (string)u.Element(ns + "LastName"),
                                    FullName = (string)u.Element(ns + "FullName"),
                                    Initials = (string)u.Element(ns + "Initials"),
                                    Title = (u.Element(ns + "Title") != null) ? (string)u.Element(ns + "Title") : "",
                                    Email = (u.Element(ns + "Email") != null) ? (string)u.Element(ns + "Email") : "",
                                    Status = (int)u.Element(ns + "Status"),
                                    DepartmentNameID = (u.Element(ns + "DepartmentNameID") != null) ? (int)u.Element(ns + "DepartmentNameID") : 0,
                                    DepartmentName = (u.Element(ns + "DepartmentName") != null) ? (string)u.Element(ns + "DepartmentName") : ""
                                };

                var store = new DataStore();
                store.Execute("TRUNCATE TABLE [TimeLogEmployee]");
                foreach (var employee in employees)
                {
                    store.Execute(string.Format("INSERT INTO [TimeLogEmployee](ID, FirstName, LastName, FullName, Initials, Title, Email, Status, DepartmentNameID, DepartmentName) " +
                                "VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}');",
                                employee.Id, employee.FirstName, employee.LastName, employee.FullName, employee.Initials,
                                employee.Title.Replace("'", "''"), employee.Email, employee.Status, employee.DepartmentNameID, employee.DepartmentName));
                }
                store.Flush();
                Console.WriteLine("{0} Employees Loaded...", employees.Count());
            
                // SHORTLIST
                //<tlp:Employee ID="555" xmlns:tlp="http://www.timelog.com/XML/Schema/tlp/v4_4">
                //<tlp:FirstName>Anders</tlp:FirstName>
                //<tlp:LastName>And</tlp:LastName>
                //<tlp:FullName>Anders And</tlp:FullName>
                //<tlp:Initials>ASC</tlp:Initials>
                //</tlp:Employee>
                // RAW LIST
                //<tlp:Employee ID="494" xmlns:tlp="http://www.timelog.com/XML/Schema/tlp/v4_4">
                //<tlp:FirstName>Jan</tlp:FirstName>
                //<tlp:LastName>Hebnes</tlp:LastName>
                //<tlp:FullName>Jan Hebnes</tlp:FullName>
                //<tlp:Initials>JHE</tlp:Initials>
                //<tlp:Title>Head of Development</tlp:Title>
                //<tlp:Email>jhe@1508.dk</tlp:Email>
                //<tlp:Phone></tlp:Phone>
                //<tlp:Mobile>12 34 56 78</tlp:Mobile>
                //<tlp:PrivatePhone></tlp:PrivatePhone>
                //<tlp:Address>Address 1 </tlp:Address>
                //<tlp:ZipCode>1000</tlp:ZipCode>
                //<tlp:City>København</tlp:City>
                //<tlp:Status>1</tlp:Status>
                //<tlp:DepartmentNameID>1</tlp:DepartmentNameID>
                //<tlp:DepartmentName>1508 A/S</tlp:DepartmentName>
                //</tlp:Employee>
                #endregion
            
                // Loading Base Related Information based on employee list
                foreach (var employee in employees)
                {
                    Console.WriteLine("{0}\t{1}", employee.Id, employee.FullName);
                    GetWorkUnit(employee.Id, DateTime.Now.AddYears(-4));
                    GetTimeOffRegistration(employee.Id, DateTime.Now.AddYears(-4));
                    GetAllocation(employee.Id);
                    GetSupportJournal(employee.Id);
                    Console.WriteLine("{3}\tRuntime: {0} - Started: {1} - Avg: {2} per employee)", DateTime.Now.Subtract(startTimer).ToString("g"), startTimer.ToString("t"), DateTime.Now.Subtract(startTimer).Milliseconds / employees.Count(), employee.Id);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                if (exception.StackTrace != null)
                    Console.WriteLine("StackTrace:" + exception.StackTrace);
                if (exception.InnerException != null)
                    Console.WriteLine("InnerException:" + exception.InnerException.ToString());
                throw;
            }
            Console.WriteLine("Done");
        }

        private static void GetTask()
        {
            XmlNode result;
            result = ServiceClient.GetTasksRaw(Config.SiteCode, Config.ApiID, Config.ApiPassword, 0, 0, -1, 0);
                //status 0 er lukkede og status 1 er aktive
            File.CreateText("TimeLog.Tasks.xml").Write(result.InnerXml);
            if (result.HasChildNodes)
            {
                XDocument xdoc = XDocument.Parse(result.OuterXml);
                XNamespace ns = "http://www.timelog.com/XML/Schema/tlp/v4_4";
                var tasks = from u in xdoc.Descendants(ns + "Task")
                    select new
                    {
                        ID = (int) u.Attribute("ID"),
                        Name = (string) u.Element(ns + "Name"),
                        ProjectID = (int) u.Element(ns + "ProjectID"),
                        Status = (int) u.Element(ns + "Status"),
                        ParentID = (int) u.Element(ns + "ParentID"),
                        IsParent = (int) u.Element(ns + "IsParent"),
                        BudgetHours = (decimal) u.Element(ns + "BudgetHours"),
                        BudgetAmount = (decimal) u.Element(ns + "BudgetAmount"),
                        IsFixedPrice = (int) u.Element(ns + "IsFixedPrice"),
                        StartDate = (DateTime) u.Element(ns + "StartDate"),
                        EndDate = (DateTime) u.Element(ns + "EndDate")
                    };
                
                var store = new DataStore();
                store.Execute("TRUNCATE TABLE [TimeLogTask]");
                foreach (var task in tasks)
                {
                    store.Execute(
                        string.Format(
                            "INSERT INTO [TimeLogTask](ID, Name, ProjectID, Status, ParentID, IsParent, BudgetHours, BudgetAmount, IsFixedPrice, StartDate, EndDate) " +
                            "VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}');",
                            task.ID, task.Name.Replace("'", "''"), task.ProjectID, task.Status, task.ParentID, task.IsParent,
                            task.BudgetHours.ToString("F", CultureInfo.InvariantCulture),
                            task.BudgetAmount.ToString("F", CultureInfo.InvariantCulture), task.IsFixedPrice,
                            task.StartDate.ToString("u"), task.EndDate.ToString("u")));
                }
                store.Flush();
                Console.WriteLine("{0} Tasks Loaded...", tasks.Count());
            }
            //<tlp:Task ID="55" xmlns:tlp="http://www.timelog.com/XML/Schema/tlp/v4_4">
            //<tlp:Name>Ferie 03/04</tlp:Name>
            //<tlp:ProjectID>15</tlp:ProjectID>
            //<tlp:Status>0</tlp:Status>
            //<tlp:ParentID>0</tlp:ParentID>
            //<tlp:IsParent>0</tlp:IsParent>
            //<tlp:BudgetHours>0.0000</tlp:BudgetHours>
            //<tlp:BudgetAmount>0.0000</tlp:BudgetAmount>
            //<tlp:IsFixedPrice>0</tlp:IsFixedPrice>
            //<tlp:StartDate>2003-05-01T00:00:00</tlp:StartDate>
            //<tlp:EndDate>2004-04-30T00:00:00</tlp:EndDate>
            //</tlp:Task>
        }

        private static void GetProject()
        {
            XmlNode result;
            result = ServiceClient.GetProjectsRaw(Config.SiteCode, Config.ApiID, Config.ApiPassword, 0, 0, 0, 0);
                //status 0 er lukkede og status 1 er aktive
            File.CreateText("TimeLog.Projects.Closed.xml").Write(result.InnerXml);
            if (result.HasChildNodes)
            {
                var projectCount = PersistProjects(result, true);
                Console.WriteLine("{0} Closed Projects Loaded...", projectCount);
            }

            result = ServiceClient.GetProjectsRaw(Config.SiteCode, Config.ApiID, Config.ApiPassword, 0, 1, 0, 0);
                //status 0 er lukkede og status 1 er aktive
            File.CreateText("TimeLog.Projects.xml").Write(result.InnerXml);
            if (result.HasChildNodes)
            {
                var projectCount = PersistProjects(result, false);
                Console.WriteLine("{0} Running Projects Loaded...", projectCount);
            }
            //<tlp:Project ID="5189" xmlns:tlp="http://www.timelog.com/XML/Schema/tlp/v4_4">
            //<tlp:Name> Femte element generator</tlp:Name>
            //<tlp:No>DIF 895.0510</tlp:No>
            //<tlp:Status>1</tlp:Status>
            //<tlp:CustomerID>1124</tlp:CustomerID>
            //<tlp:CustomerName>Danmarks Idræts-Forbund</tlp:CustomerName>
            //<tlp:CustomerNo>DIF 895</tlp:CustomerNo>
            //<tlp:PMID>521</tlp:PMID>
            //<tlp:PMInitials>LBH</tlp:PMInitials>
            //<tlp:PMFullName>Linda Hansen</tlp:PMFullName>
            //<tlp:ProjectTypeID>279</tlp:ProjectTypeID>
            //<tlp:ProjectTypeName>5. Website</tlp:ProjectTypeName>
            //<tlp:ProjectCategoryID>17</tlp:ProjectCategoryID>
            //<tlp:ProjectCategoryName>B. Projekt</tlp:ProjectCategoryName>
            //</tlp:Project>
        }

        private static void GetCustomer()
        {
            var result = ServiceClient.GetCustomersRaw(Config.SiteCode, Config.ApiID, Config.ApiPassword, 0, -1, 0, string.Empty);
                //status 0 er lukkede og status 1 er aktive
            File.CreateText("TimeLog.Customers.xml").Write(result.InnerXml);
            if (result.HasChildNodes)
            {
                XDocument xdoc = XDocument.Parse(result.OuterXml);
                XNamespace ns = "http://www.timelog.com/XML/Schema/tlp/v4_4";
                var customers = from u in xdoc.Descendants(ns + "Customer")
                    select new
                    {
                        Id = (int) u.Attribute("ID"),
                        Name = (string) u.Element(ns + "Name"),
                        No = (string) u.Element(ns + "No"),
                        CustomerStatusID = (int) u.Element(ns + "CustomerStatusID"),
                        CustomerStatus = (string) u.Element(ns + "CustomerStatus"),
                        Email = (string) u.Element(ns + "Email"),
                        WebPage = (string) u.Element(ns + "WebPage"),
                        VATNo = (string) u.Element(ns + "VATNo"),
                        Comment = (u.Element(ns + "Comment") != null) ? (string) u.Element(ns + "Comment") : "",
                        IndustryID = (u.Element(ns + "IndustryID") != null) ? (int) u.Element(ns + "IndustryID") : 0,
                        IndustryName = (u.Element(ns + "IndustryName") != null) ? (string) u.Element(ns + "IndustryName") : ""
                    };

                var store = new DataStore();
                store.Execute("TRUNCATE TABLE [TimeLogCustomer]");
                foreach (var customer in customers)
                {
                    store.Execute(
                        string.Format(
                            "INSERT INTO [TimeLogCustomer](ID, Name, No, CustomerStatusID, CustomerStatus, Email, WebPage, VATNo, Comment, IndustryID, IndustryName) " +
                            "VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}');",
                            customer.Id, customer.Name.Replace("'", "''"), customer.No, customer.CustomerStatusID,
                            customer.CustomerStatus,
                            customer.Email, customer.WebPage, customer.VATNo, customer.Comment.Replace("'", "''"),
                            customer.IndustryID, customer.IndustryName));
                }
                store.Flush();
                Console.WriteLine("{0} Customers Loaded...", customers.Count());
            }
            //<tlp:Customer ID="1162" xmlns:tlp="http://www.timelog.com/XML/Schema/tlp/v4_4">
            //<tlp:Name>Company X</tlp:Name>
            //<tlp:No>ALS 555</tlp:No>
            //<tlp:CustomerStatusID>158</tlp:CustomerStatusID>
            //<tlp:CustomerStatus>Samarbejdspartner</tlp:CustomerStatus>
            //<tlp:Address1>Address1</tlp:Address1>
            //<tlp:Address2></tlp:Address2>
            //<tlp:Address3></tlp:Address3>
            //<tlp:ZipCode>1000</tlp:ZipCode>
            //<tlp:City>København</tlp:City>
            //<tlp:State></tlp:State>
            //<tlp:Country>Denmark</tlp:Country>
            //<tlp:Phone>12 34 56 78</tlp:Phone>
            //<tlp:Fax></tlp:Fax>
            //<tlp:Email>info@company.dk</tlp:Email>
            //<tlp:WebPage>www.company.dk</tlp:WebPage>
            //<tlp:VATNo>12345678</tlp:VATNo>
            //<tlp:Comment></tlp:Comment>
            //<tlp:AccountManagerID>361</tlp:AccountManagerID>
            //<tlp:AccountManagerFullName>John Hansen</tlp:AccountManagerFullName>
            //<tlp:IndustryID>319</tlp:IndustryID>
            //<tlp:IndustryName>Privat</tlp:IndustryName>
            //</tlp:Customer>
        }

        private static void GetWorkUnit(int employeeID, DateTime? startDate)
        {
            XmlNode result = ServiceClient.GetWorkUnitsRaw(Config.SiteCode, Config.ApiID, Config.ApiPassword, 0, employeeID, 0, 0, 0, 0, startDate, DateTime.Now);
            File.CreateText(string.Format("TimeLog.EmployeeID{0}.WorkUnits.xml", employeeID)).Write(result.InnerXml);
            if (result.HasChildNodes)
            {
                XDocument xdoc = XDocument.Parse(result.OuterXml);
                XNamespace ns = "http://www.timelog.com/XML/Schema/tlp/v4_4";
                var workunits = from u in xdoc.Descendants(ns + "WorkUnit")
                    select new
                    {
                        ID = (int) u.Attribute("ID"),
                        EmployeeID = (int) u.Element(ns + "EmployeeID"),
                        EmployeeInitials = (string) u.Element(ns + "EmployeeInitials"),
                        EmployeeFirstName = (string) u.Element(ns + "EmployeeFirstName"),
                        EmployeeLastName = (string) u.Element(ns + "EmployeeLastName"),
                        AllocationID = (int) u.Element(ns + "AllocationID"),
                        TaskID = (int) u.Element(ns + "TaskID"),
                        ProjectID = (int) u.Element(ns + "ProjectID"),
                        Date = (DateTime) u.Element(ns + "Date"),
                        Note = (string) u.Element(ns + "Note"),
                        AdditionalTextField = (string) u.Element(ns + "AdditionalTextField"),
                        RegHours = (decimal) u.Element(ns + "RegHours"),
                        Billable = (decimal) u.Element(ns + "Billable"),
                        InvHours = (decimal) u.Element(ns + "InvHours"),
                        CostAmount = (decimal) u.Element(ns + "CostAmount"),
                        RegAmount = (decimal) u.Element(ns + "RegAmount"),
                        InvAmount = (decimal) u.Element(ns + "InvAmount")
                    };

                var store = new DataStore();
                store.Execute(string.Format("DELETE FROM [TimeLogWorkUnit] WHERE EmployeeID = '{0}'", employeeID));
                foreach (var workunit in workunits)
                {
                    store.Execute(
                        string.Format(
                            "INSERT INTO [TimeLogWorkUnit](ID, EmployeeID, EmployeeInitials, EmployeeFirstName, EmployeeLastName, AllocationID, TaskID, ProjectID, Date, Note, AdditionalTextField, RegHours, Billable, InvHours, CostAmount, RegAmount, InvAmount) " +
                            "VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}');",
                            workunit.ID, workunit.EmployeeID, workunit.EmployeeInitials, workunit.EmployeeFirstName,
                            workunit.EmployeeLastName, workunit.AllocationID, workunit.TaskID, workunit.ProjectID,
                            workunit.Date.ToString("u", CultureInfo.InvariantCulture), workunit.Note.Replace("'", "''"),
                            workunit.AdditionalTextField.Replace("'", "''"),
                            workunit.RegHours.ToString("F", CultureInfo.InvariantCulture),
                            workunit.Billable.ToString("F", CultureInfo.InvariantCulture),
                            workunit.InvHours.ToString("F", CultureInfo.InvariantCulture),
                            workunit.CostAmount.ToString("F", CultureInfo.InvariantCulture),
                            workunit.RegAmount.ToString("F", CultureInfo.InvariantCulture),
                            workunit.InvAmount.ToString("F", CultureInfo.InvariantCulture)));
                }
                store.Flush();
                Console.WriteLine("{1}\t{0} WorkUnits Loaded for Employee...", workunits.Count(), employeeID);
            }
            //<tlp:WorkUnit ID="411631" xmlns:tlp="http://www.timelog.com/XML/Schema/tlp/v4_4">
            //<tlp:EmployeeID>564</tlp:EmployeeID>
            //<tlp:EmployeeInitials>MBH</tlp:EmployeeInitials>
            //<tlp:EmployeeFirstName>Maja</tlp:EmployeeFirstName>
            //<tlp:EmployeeLastName>Hanse</tlp:EmployeeLastName>
            //<tlp:AllocationID>42413</tlp:AllocationID>
            //<tlp:TaskID>24668</tlp:TaskID>
            //<tlp:ProjectID>5164</tlp:ProjectID>
            //<tlp:Date>2013-10-21T00:00:00</tlp:Date>
            //<tlp:Note></tlp:Note>
            //<tlp:AdditionalTextField></tlp:AdditionalTextField>
            //<tlp:RegHours>0.2500</tlp:RegHours>
            //<tlp:Billable>0.2500</tlp:Billable>
            //<tlp:InvHours>0.0000</tlp:InvHours>
            //<tlp:CostAmount>162.5000</tlp:CostAmount>
            //<tlp:RegAmount>0.0000</tlp:RegAmount>
            //<tlp:InvAmount>0.0000</tlp:InvAmount>
            //</tlp:WorkUnit>
        }

        private static void GetTimeOffRegistration(int employeeID, DateTime startTime)
        {
            XmlNode result = ServiceClient.GetTimeOffRegistrationsRaw(Config.SiteCode, Config.ApiID, Config.ApiPassword, employeeID, 0, startTime, DateTime.Now);
            File.CreateText(string.Format("TimeLog.EmployeeID{0}.TimeOffRegistrations.xml", employeeID)).Write(result.InnerXml);
            if (result.HasChildNodes)
            {
                XDocument xdoc = XDocument.Parse(result.OuterXml);
                XNamespace ns = "http://www.timelog.com/XML/Schema/tlp/v4_4";
                var timeoffregistrations = from u in xdoc.Descendants(ns + "TimeOffRegistration")
                    select new
                    {
                        ID = (int) u.Attribute("ID"),
                        EmployeeID = (int) u.Element(ns + "EmployeeID"),
                        EmployeeInitials = (string) u.Element(ns + "EmployeeInitials"),
                        EmployeeFirstName = (string) u.Element(ns + "EmployeeFirstName"),
                        EmployeeLastName = (string) u.Element(ns + "EmployeeLastName"),
                        TimeOffCode = (string) u.Element(ns + "TimeOffCode"),
                        TimeOffName = (string) u.Element(ns + "TimeOffName"),
                        ProjectID = (u.Element(ns + "ProjectID") != null) ? (int) u.Element(ns + "ProjectID") : 0,
                        Date = (u.Element(ns + "Date") != null) ? u.Element(ns + "Date").Value.Replace("T", " ") : "",
                        RegHours =
                            (u.Element(ns + "RegHours") != null)
                                ? ConvertFloatValue(u.Element(ns + "RegHours").Value).ToString()
                                : "0"
                    };

                var store = new DataStore();
                store.Execute(string.Format("DELETE FROM [TimeLogTimeOffRegistration] WHERE EmployeeID = '{0}'", employeeID));
                foreach (var timeoffregistration in timeoffregistrations)
                {
                    store.Execute(
                        string.Format(
                            "INSERT INTO [TimeLogTimeOffRegistration](ID, EmployeeID, EmployeeInitials, EmployeeFirstName, EmployeeLastName, TimeOffCode, TimeOffName, ProjectID, Date, RegHours) " +
                            "VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}');",
                            timeoffregistration.ID, timeoffregistration.EmployeeID, timeoffregistration.EmployeeInitials,
                            timeoffregistration.EmployeeFirstName, timeoffregistration.EmployeeLastName,
                            timeoffregistration.TimeOffCode, timeoffregistration.TimeOffName, timeoffregistration.ProjectID,
                            timeoffregistration.Date, timeoffregistration.RegHours));
                }
                store.Flush();
                Console.WriteLine("{1}\t{0} TimeOffRegistrations Loaded for Employee...", timeoffregistrations.Count(), employeeID);
            }
            //<tlp:TimeOffRegistration ID="414829" xmlns:tlp="http://www.timelog.com/XML/Schema/tlp/v4_4">
            //<tlp:EmployeeID>409</tlp:EmployeeID>
            //<tlp:EmployeeInitials>HTX</tlp:EmployeeInitials>
            //<tlp:EmployeeFirstName>Helle</tlp:EmployeeFirstName>
            //<tlp:EmployeeLastName>Hansen</tlp:EmployeeLastName>
            //<tlp:TimeOffCode>INT 900.0001</tlp:TimeOffCode>
            //<tlp:TimeOffName>Barsel</tlp:TimeOffName>
            //<tlp:Date>2013-11-18T00:00:00</tlp:Date>
            //<tlp:RegHours>7.500000000000000e+000</tlp:RegHours>
            //</tlp:TimeOffRegistration>
        }

        private static void GetAllocation(int employeeID)
        {
            XmlNode result = ServiceClient.GetAllocationsRaw(Config.SiteCode, Config.ApiID, Config.ApiPassword, 0, 0, employeeID, 0);
            File.CreateText(string.Format("TimeLog.EmployeeID{0}.Allocations.xml", employeeID)).Write(result.InnerXml);
            if (result.HasChildNodes)
            {
                XDocument xdoc = XDocument.Parse(result.OuterXml);
                XNamespace ns = "http://www.timelog.com/XML/Schema/tlp/v4_4";
                var allocations = from u in xdoc.Descendants(ns + "Allocation")
                    select new
                    {
                        ID = (int) u.Attribute("ID"),
                        ProjectID = (u.Element(ns + "ProjectID") != null) ? (int) u.Element(ns + "ProjectID") : 0,
                        TaskID = (u.Element(ns + "TaskID") != null) ? (int) u.Element(ns + "TaskID") : 0,
                        EmployeeID = (int) u.Element(ns + "EmployeeID"),
                        AllocatedHours =
                            (u.Element(ns + "AllocatedHours") != null)
                                ? ConvertFloatValue(u.Element(ns + "AllocatedHours").Value)
                                : "0",
                        HourlyRate =
                            (u.Element(ns + "HourlyRate") != null) ? ConvertFloatValue(u.Element(ns + "HourlyRate").Value) : "0",
                        TaskIsFixedPrice = (string) u.Element(ns + "TaskIsFixedPrice")
                    };

                var store = new DataStore();
                store.Execute(string.Format("DELETE FROM [TimeLogAllocation] WHERE EmployeeID = '{0}'", employeeID));
                foreach (var allocation in allocations)
                {
                    store.Execute(
                        string.Format(
                            "INSERT INTO [TimeLogAllocation](ID, ProjectID, TaskID, EmployeeID, AllocatedHours, HourlyRate, TaskIsFixedPrice) " +
                            "VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}');",
                            allocation.ID, allocation.ProjectID, allocation.TaskID, allocation.EmployeeID,
                            allocation.AllocatedHours, allocation.HourlyRate, allocation.TaskIsFixedPrice));
                }
                store.Flush();
                Console.WriteLine("{1}\t{0} Allocations Loaded for Employee...", allocations.Count(), employeeID);
            }
            //<tlp:Allocation ID="16" xmlns:tlp="http://www.timelog.com/XML/Schema/tlp/v4_4">
            // <tlp:ProjectID>15</tlp:ProjectID>
            // <tlp:TaskID>55</tlp:TaskID>
            // <tlp:EmployeeID>359</tlp:EmployeeID>
            // <tlp:AllocatedHours>0.0000</tlp:AllocatedHours>
            // <tlp:HourlyRate>0.0000</tlp:HourlyRate>
            // <tlp:TaskIsFixedPrice>0</tlp:TaskIsFixedPrice>
            //</tlp:Allocation>
        }

        private static void GetSupportJournal(int employeeID)
        {
            XmlNode result = ServiceClient.GetSupportJournalRaw(Config.SiteCode, Config.ApiID, Config.ApiPassword, 0, employeeID, 0, 0);
            File.CreateText(string.Format("TimeLog.EmployeeID{0}.SupportJournals.xml", employeeID)).Write(result.InnerXml);
            if (result.HasChildNodes)
            {
                XDocument xdoc = XDocument.Parse(result.OuterXml);
                XNamespace ns = "http://www.timelog.com/XML/Schema/tlp/v4_4";
                var supportjournals = from u in xdoc.Descendants(ns + "SupportJournal")
                    select new
                    {
                        ID = (int) u.Attribute("ID"),
                        Date = (u.Element(ns + "Date") != null) ? u.Element(ns + "Date").Value.Replace("T", " ") : "",
                        StartTime = (string) u.Element(ns + "StartTime"),
                        EndTime = (string) u.Element(ns + "EndTime"),
                        RegMinutes = (string) u.Element(ns + "RegMinutes"),
                        Comment = (string) u.Element(ns + "Comment"),
                        RegHours = (string) u.Element(ns + "RegHours"),
                        InvHours = (string) u.Element(ns + "InvHours"),
                        CostAmount = (string) u.Element(ns + "CostAmount"),
                        RegAmount = (string) u.Element(ns + "RegAmount"),
                        InvAmount = (string) u.Element(ns + "InvAmount"),
                        EmployeeID = (int) u.Element(ns + "EmployeeID"),
                        EmployeeInitials = (string) u.Element(ns + "EmployeeInitials"),
                        EmployeeFullName = (string) u.Element(ns + "EmployeeFullName"),
                        CustomerID = (int) u.Element(ns + "CustomerID"),
                        CustomerName = (string)u.Element(ns + "CustomerName"),
                        CustomerNo = (string) u.Element(ns + "CustomerNo"),
                        SupportCaseID = (string) u.Element(ns + "SupportCaseID"),
                        SupportCaseHeader = (string) u.Element(ns + "SupportCaseHeader"),
                        SupportCaseNo = (string) u.Element(ns + "SupportCaseNo"),
                        SupportContractID =
                            (u.Element(ns + "SupportContractID") != null) ? (string) u.Element(ns + "SupportContractID") : "",
                        SupportContractName =
                            (u.Element(ns + "SupportContractName") != null)
                                ? (string) u.Element(ns + "SupportContractName")
                                : "",
                        SupportContractNo =
                            (u.Element(ns + "SupportContractNo") != null) ? (string) u.Element(ns + "SupportContractNo") : ""
                    };

                var store = new DataStore();
                store.Execute(string.Format("DELETE FROM [TimeLogSupportJournal] WHERE EmployeeID = '{0}'", employeeID));
                foreach (var supportjournal in supportjournals)
                {
                    store.Execute(
                        string.Format(
                            "INSERT INTO [TimeLogSupportJournal]([ID],[Date],[StartTime],[EndTime],[RegMinutes],[Comment],[RegHours],[InvHours],[CostAmount],[RegAmount],[InvAmount],[EmployeeID],[EmployeeInitials],[EmployeeFullName],[CustomerID],[CustomerName],[CustomerNo],[SupportCaseID],[SupportCaseHeader],[SupportCaseNo],[SupportContractID],[SupportContractName],[SupportContractNo]) " +
                            "VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}');",
                            supportjournal.ID, supportjournal.Date, supportjournal.StartTime, supportjournal.EndTime,
                            supportjournal.RegMinutes, supportjournal.Comment.Replace("'", "''"), supportjournal.RegHours,
                            supportjournal.InvHours, supportjournal.CostAmount, supportjournal.RegAmount,
                            supportjournal.InvAmount, supportjournal.EmployeeID, supportjournal.EmployeeInitials,
                            supportjournal.EmployeeFullName, supportjournal.CustomerID,
                            supportjournal.CustomerName.Replace("'", "''"), supportjournal.CustomerNo,
                            supportjournal.SupportCaseID, supportjournal.SupportCaseHeader.Replace("'", "''"),
                            supportjournal.SupportCaseNo, supportjournal.SupportContractID,
                            supportjournal.SupportContractName.Replace("'", "''"), supportjournal.SupportContractNo));
                }
                store.Flush();
                Console.WriteLine("{1}\t{0} SupportJournals Loaded for Employee...", supportjournals.Count(), employeeID);
            }
            //<tlp:SupportJournal ID="17697" xmlns:tlp="http://www.timelog.com/XML/Schema/tlp/v4_4">
            // <tlp:Date>2012-11-01T00:00:00</tlp:Date>
            // <tlp:StartTime>10:00</tlp:StartTime>
            // <tlp:EndTime>13:30</tlp:EndTime>
            // <tlp:RegMinutes>210</tlp:RegMinutes>
            // <tlp:Comment>Kunde pr&amp;#230;sentation af unikke sider incl. transport</tlp:Comment>
            // <tlp:RegHours>3.500000</tlp:RegHours>
            // <tlp:InvHours>3.500000</tlp:InvHours>
            // <tlp:CostAmount>1925</tlp:CostAmount>
            // <tlp:RegAmount>3850</tlp:RegAmount>
            // <tlp:InvAmount>2275</tlp:InvAmount>
            // <tlp:EmployeeID>467</tlp:EmployeeID>
            // <tlp:EmployeeInitials>CHX</tlp:EmployeeInitials>
            // <tlp:EmployeeFullName>Casper Hansen</tlp:EmployeeFullName>
            // <tlp:CustomerID>1885</tlp:CustomerID>
            // <tlp:CustomerName>Firma</tlp:CustomerName>
            // <tlp:CustomerNo>XXX 1000</tlp:CustomerNo>
            // <tlp:SupportCaseID>411</tlp:SupportCaseID>
            // <tlp:SupportCaseHeader>Support: Kunde A/S</tlp:SupportCaseHeader>
            // <tlp:SupportCaseNo></tlp:SupportCaseNo>
            // <tlp:SupportContractID>266</tlp:SupportContractID>
            // <tlp:SupportContractName>Kunde 500kr</tlp:SupportContractName>
            // <tlp:SupportContractNo></tlp:SupportContractNo>
            // </tlp:SupportJournal>
        }

        /// <summary>
        /// Handle x.xxx+3000 or 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string ConvertFloatValue(string input)
        {
            if (input.Length <= input.LastIndexOf("0000") + 4) return input;
            return input.Remove(input.LastIndexOf("0000") + 4);
        }

        private static int PersistProjects(XmlNode result, bool truncateTable)
        {
            DateTime startTimer = DateTime.Now;
            XDocument xdoc = XDocument.Parse(result.OuterXml);
            XNamespace ns = "http://www.timelog.com/XML/Schema/tlp/v4_4";
            var projects = from u in xdoc.Descendants(ns + "Project")
                select new
                {
                    ID = (int) u.Attribute("ID"),
                    Name = (string) u.Element(ns + "Name"),
                    No = (string) u.Element(ns + "No"),
                    Status = (int) u.Element(ns + "Status"),
                    CustomerID = (int) u.Element(ns + "CustomerID"),
                    CustomerName = (string) u.Element(ns + "CustomerName"),
                    CustomerNo = (string) u.Element(ns + "CustomerNo"),
                    PMID = (int) u.Element(ns + "PMID"),
                    PMInitials = (string) u.Element(ns + "PMInitials"),
                    PMFullName = (string) u.Element(ns + "PMFullName"),
                    ProjectTypeID = (int) u.Element(ns + "ProjectTypeID"),
                    ProjectTypeName = (string) u.Element(ns + "ProjectTypeName"),
                    ProjectCategoryID =
                        (u.Element(ns + "ProjectCategoryID") != null) ? (int) u.Element(ns + "ProjectCategoryID") : 0,
                    ProjectCategoryName =
                        (u.Element(ns + "ProjectCategoryName") != null) ? (string) u.Element(ns + "ProjectCategoryName") : ""
                };

            var store = new DataStore();
            if (truncateTable)
            {
                store.Execute("TRUNCATE TABLE [TimeLogProject]");
            }
            foreach (var project in projects)
            {
                store.Execute(string.Format(
                        "INSERT INTO [TimeLogProject](ID, Name, No, Status, CustomerID, CustomerName, CustomerNo, PMID, PMInitials, PMFullName, ProjectTypeID, ProjectTypeName, ProjectCategoryID, ProjectCategoryName) " +
                        "VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}');",
                        project.ID, project.Name.Replace("'", "''"), project.No, project.Status, project.CustomerID,
                        project.CustomerName.Replace("'", "''"), project.CustomerNo, project.PMID, project.PMInitials,
                        project.PMFullName, project.ProjectTypeID, project.ProjectTypeName.Replace("'", "''"),
                        project.ProjectCategoryID, project.ProjectCategoryName.Replace("'", "''")));
            }
            store.Flush();
            return projects.Count();
        }

        #endregion Methods

        #region Nested Types

        public static class Config
        {
            #region Properties

            public static string ApiID
            {
                get
                {
                    return ConfigurationManager.AppSettings["TimeLog.ApiID"];
                }
            }

            public static string ApiPassword
            {
                get
                {
                    return ConfigurationManager.AppSettings["TimeLog.ApiPassword"];
                }
            }

            public static string ConnectionString
            {
                get
                {
                    return ConfigurationManager.ConnectionStrings["TimeLog"].ConnectionString;
                }
            }

            public static string SiteCode
            {
                get
                {
                    return ConfigurationManager.AppSettings["TimeLog.SiteCode"];
                }
            }

            public static string Url
            {
                get
                {
                    return ConfigurationManager.AppSettings["TimeLog.Url"];
                }
            }

            #endregion Properties
        }

        #endregion Nested Types
    }
}