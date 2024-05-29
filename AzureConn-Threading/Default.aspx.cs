#define SLEEP
#define METHOD
#define WORKER

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace AzureConn
{
    public partial class Default : System.Web.UI.Page
    {
        public string dateFormat = "yyyy-MM-dd HH:mm:ss";
        public int sharedVariableForThreads = 123;

        public string fullAddress = "tcp:database.database.windows.net";
        public string cNameAddress = "";
        public string threadCount = "3";
        public string databaseName = "BookStores";
        public string userId = "username";
        public string password = "password";

        public string query = "SELECT so.name FROM sysobjects so WHERE so.xtype = 'U' ";
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                txtFullAddress.Text = fullAddress;
                txtCNameAddress.Text = cNameAddress;
                txtThreadCount.Text = threadCount;
                txtDatabaseName.Text = databaseName;
                txtUserId.Text = userId;
                txtPassword.Text = "password";
                txtQuery.Text = query;
                StartedTime.Text = DateTime.Now.ToString();
            }
        }

        protected void btnFullAddress_Click(object sender, EventArgs e)
        {
            try
            {
                lblServerName.Text = txtFullAddress.Text;
                txtResults.Text = Get_SQL_Data(lblServerName.Text, txtQuery.Text);
                FinishedTime.Text = DateTime.Now.ToString();
            }
            catch (Exception ex)
            {
                lblServerName.Text = ex.Message;
            }
        }

        protected void btnCNameAddress_Click(object sender, EventArgs e)
        {
            lblServerName.Text = txtCNameAddress.Text;
            txtResults.Text = Get_SQL_Data(lblServerName.Text, txtQuery.Text);
        }

        private string Get_SQL_Data(string serverName, string queryString)
        {
            string outString = string.Empty;
            string sqlString = string.Empty;
            string query = queryString;

            if (txtPassword.Text.Length < 1) txtPassword.Text = "password";
            string password = ConfigurationManager.AppSettings[txtPassword.Text];
            bool contains = ("web,prod").IndexOf(txtPassword.Text, StringComparison.OrdinalIgnoreCase) >= 0;

            SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder();
            sb.DataSource = serverName;
            sb.DataSource = txtFullAddress.Text;
            sb.UserID = txtUserId.Text;
            sb.InitialCatalog = txtDatabaseName.Text;
            sb.Password = contains ? password : txtPassword.Text;
            sb.TrustServerCertificate = false;
            SqlConnection cn = new SqlConnection(sb.ConnectionString);

            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = query;
            cmd.Connection = cn;

            try
            {
                SqlDataAdapter sqlda = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                sqlda.Fill(ds, "author");
                DataTable dt = ds.Tables["author"];
                int r1 = dt.Rows.Count;

                bool isGood = (dt != null && dt.Rows.Count > 0) ? true : false;
                if (isGood)
                {
                    if (rdoTrue.Checked)
                        outString = Newtonsoft.Json.JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                    else
                        outString = Newtonsoft.Json.JsonConvert.SerializeObject(dt);
                }

                dt.Dispose();
            }
            catch (Exception ex)
            {
                outString = ex.Message;
            }

#if noSLEEP
            //**TEST**
            int sleep = Convert.ToInt32(ConfigurationManager.AppSettings["sleep"]);
            System.Threading.Thread.Sleep(sleep);  // 5 minutes: 300,000
#endif
            return outString;
        }

        /* ----------------------------------------------------------------------------------------*/
        /* THREAD                                                                                  */
        /* ----------------------------------------------------------------------------------------*/

        protected void btnThread_Click(object sender, EventArgs e)
        {
            lblServerName.Text = "Threading...";
            txtResults.Text = "Thread: ";
            StartedTime.Text = DateTime.Now.ToString();
            Stopwatch elapsedStopWatch = Stopwatch.StartNew();
#if METHOD
            txtResults.Text += Create_Threads_Method(txtThreadCount.Text);
#endif
#if noWORKER
            txtResults.Text += Create_Threads_Class(txtThreadCount.Text);
#endif
            elapsedStopWatch.Stop();
            FinishedTime.Text = DateTime.Now.ToString();

            TimeSpan ets = elapsedStopWatch.Elapsed;
            txtResults.Text += string.Format("\nTotal {0} minutes {1} seconds {2} ms\n", ets.Minutes, ets.Seconds, ets.Milliseconds);
            elapsedStopWatch.Reset();
        }

        //-----------------------------------------------------------------------------------------METHOD
        private string Create_Threads_Method(string count)
        {
            int threadsCount = Convert.ToInt32(count);
            int totalProcessedCount = 0;

            int[] processedCounts = new int[threadsCount];
            Thread[] threads = new Thread[threadsCount];

            for (int i = 0; i < threadsCount; i++)
            {
                int index = i;

                Params param = new Params();
                string guid = System.Guid.NewGuid().ToString();
                param.Code = guid.Substring(0, 8);

                Random rnd = new Random();
                param.Count = rnd.Next(5, 20);

                Debug.WriteLine("Thread:{0}, Count: {1}", param.Code, param.Count);

                threads[i] = new Thread(() => processedCounts[index] = DoWork(param));
                threads[i].Start();
            }

            foreach (Thread thread in threads)
            {
                if(rdoJoinTrue.Checked)
                    thread.Join();
            }

            foreach (int cnt in processedCounts)
            {
                totalProcessedCount += cnt;
            }

            Console.WriteLine("Total processed count: " + totalProcessedCount);
            Debug.WriteLine("Total processed count: " + totalProcessedCount);
            return totalProcessedCount.ToString();
        }

        //private async Task<int> DoWork(object data)
        private int DoWork(object data)
        {
            int ProcessedCount = 0;

            Params param = data as Params;
            string code = param.Code;
            int count = param.Count;

            for (int i = 0; i < count; i++)
            {
                ProcessedCount++;
                System.Threading.Thread.Sleep(500);     //0.5 seconds 
                //await Task.Delay(500);                  //non-blocking 
                //Task.Delay(500).Wait();                 //blocking
                Debug.WriteLine(code + ": " + ProcessedCount.ToString());
                Debug.WriteLine("sharedVariableForThreads: " + sharedVariableForThreads.ToString());
            }
            return count;
        }


        //-----------------------------------------------------------------------------------------WORKER
        private string Create_Threads_Class(string threadsCount)
        {
            int count = Convert.ToInt32(threadsCount);
            int totalProcessedCount = 0;

            List<Worker> workers = new List<Worker>();
            List<Thread> threads = new List<Thread>();

            for (int i = 0; i < count; i++)
            {
                Worker worker = new Worker();
                workers.Add(worker);

                Thread thread = new Thread(worker.DoWork);
                threads.Add(thread);

                Params param = new Params();
                string guid = System.Guid.NewGuid().ToString();
                param.Code = guid.Substring(0,8);

                Random rnd = new Random();
                param.Count = rnd.Next(5, 20);

                Debug.WriteLine("Thread:{0}, Count: {1}", param.Code, param.Count);

                thread.Start(param);
            }

            foreach (Thread thread in threads)
            {
                if (rdoJoinTrue.Checked)
                    thread.Join();      //wait for each thread is finished
            }

            foreach (Worker worker in workers)
            {
                totalProcessedCount += worker.ProcessedCount;
            }

            Console.WriteLine("Total processed count: " + totalProcessedCount);
            Debug.WriteLine("Total processed count: " + totalProcessedCount);
            return totalProcessedCount.ToString();

        }


        /* ----------------------------------------------------------------------------------------*/
        /* Task.Run                                                                                */
        /* ----------------------------------------------------------------------------------------*/
        protected void btnTaskRun_Click(object sender, EventArgs e)
        {
            lblServerName.Text = "Task.Run...";
            txtResults.Text = "TaskRun: "; 
            StartedTime.Text = DateTime.Now.ToString();
            Stopwatch elapsedStopWatch = Stopwatch.StartNew();
#if METHOD
            txtResults.Text += Create_TaskRun_Method(txtThreadCount.Text).Result;
#endif
#if noWORKER
            txtResults.Text += Create_TaskRun_Class(txtThreadCount.Text).Result;
#endif
            elapsedStopWatch.Stop();
            FinishedTime.Text = DateTime.Now.ToString();

            TimeSpan ets = elapsedStopWatch.Elapsed;
            txtResults.Text += string.Format("\nTotal {0} minutes {1} seconds {2} ms\n", ets.Minutes, ets.Seconds, ets.Milliseconds);
            elapsedStopWatch.Reset();
        }

        //-----------------------------------------------------------------------------------------METHOD
        private Task<string> Create_TaskRun_Method(string count)
        {
            int tasksCount = Convert.ToInt32(count);
            int totalProcessedCount = 0;
            Random rnd = new Random();

            int[] processedCounts = new int[tasksCount];
            Task[] tasks = new Task[tasksCount];

            for (int i = 0; i < tasksCount; i++)
            {
                int index = i;

                Params param = new Params();
                string guid = System.Guid.NewGuid().ToString();
                param.Code = guid.Substring(0, 8);

                param.Count = rnd.Next(5, 20);

                Debug.WriteLine("Task:{0}, Count:{1}", param.Code, param.Count);

                Task task = Task.Run(() =>
                {
                    try
                    {
                        processedCounts[index] = DoWork(param);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception in DoWork: {0}", ex.Message);
                    }
                });
                tasks[i] = task;
            }

            try
            {
                if (rdoJoinTrue.Checked)
                    Task.WhenAll(tasks).Wait();                 //blocking
            }
            catch (AggregateException ex)
            {
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Debug.WriteLine("Exception in tasks: {0}", innerEx.Message);
                }
            }

            for (int i = 0; i < tasksCount; i++)
            {
                Debug.WriteLine("Task: {0} status: {1}", tasks[i].Id, tasks[i].Status);
            }

            foreach (int cnt in processedCounts)
            {
                totalProcessedCount += cnt;
            }

            Console.WriteLine("Total processed count: " + totalProcessedCount);
            Debug.WriteLine("Total processed count: " + totalProcessedCount);

            return Task.FromResult(totalProcessedCount.ToString());

        }

        //-----------------------------------------------------------------------------------------WORKER
        private Task<string> Create_TaskRun_Class(string threadsCount)
        {
            int count = Convert.ToInt32(threadsCount);
            int totalProcessedCount = 0;

            List<Worker> workers = new List<Worker>();
            List<Task> tasks = new List<Task>();
            Random rnd = new Random();

            for (int i = 0; i < count; i++)
            {
                Worker worker = new Worker();
                workers.Add(worker);

                Params param = new Params();
                string guid = System.Guid.NewGuid().ToString();
                param.Code = guid.Substring(0, 8);

                param.Count = rnd.Next(5, 20);

                Debug.WriteLine("Thread:{0}, Count: {1}", param.Code, param.Count);

                Task task = Task.Run(() => worker.DoWork(param));
                tasks.Add(task);

            }

            try
            {
                if (rdoJoinTrue.Checked)
                    Task.WhenAll(tasks).Wait();
            }
            catch (AggregateException ex)
            {
                Console.WriteLine($"WhenAll.Wait(): {ex.ToString()}");
            }

            foreach (Worker worker in workers)
            {
                totalProcessedCount += worker.ProcessedCount;
            }

            Console.WriteLine("Total processed count: " + totalProcessedCount);
            Debug.WriteLine("Total processed count: " + totalProcessedCount);
            return Task.FromResult(totalProcessedCount.ToString());

        }


        /* ----------------------------------------------------------------------------------------*/
        /* Task.Parallel: blocking code                                                            */
        /* ----------------------------------------------------------------------------------------*/
        protected void btnParallel_Click(object sender, EventArgs e)
        {
            lblServerName.Text = "Task.Run...";
            txtResults.Text = "Parallel: ";
            StartedTime.Text = DateTime.Now.ToString();
            Stopwatch elapsedStopWatch = Stopwatch.StartNew();
#if noMETHOD
            txtResults.Text += Create_Parallel_Method(txtThreadCount.Text);
#endif
#if WORKER
            txtResults.Text += Create_Parallel_Class(txtThreadCount.Text);
#endif
            elapsedStopWatch.Stop();
            FinishedTime.Text = DateTime.Now.ToString();

            TimeSpan ets = elapsedStopWatch.Elapsed;
            txtResults.Text += string.Format("\nTotal {0} minutes {1} seconds {2} ms\n", ets.Minutes, ets.Seconds, ets.Milliseconds);
            elapsedStopWatch.Reset();
        }

        //-----------------------------------------------------------------------------------------METHOD
        private string Create_Parallel_Method(string count)
        {
            int tasksCount = Convert.ToInt32(count);
            int totalProcessedCount = 0;
            Random rnd = new Random();

            int[] processedCounts = new int[tasksCount];
            Task[] tasks = new Task[tasksCount];

            Parallel.For(0, tasksCount, i =>
            {
                int index = i;

                Params param = new Params();
                string guid = System.Guid.NewGuid().ToString();
                param.Code = guid.Substring(0, 8);

                param.Count = rnd.Next(5, 20);

                Debug.WriteLine("Task:{0}, Count: {1}", param.Code, param.Count);
                processedCounts[index] = DoWork(param);
            });

            foreach (int cnt in processedCounts)
            {
                totalProcessedCount += cnt;
            }

            Console.WriteLine("Total processed count: " + totalProcessedCount);
            Debug.WriteLine("Total processed count: " + totalProcessedCount);

            return totalProcessedCount.ToString();

        }

        //-----------------------------------------------------------------------------------------WORKER
        private string Create_Parallel_Class(string Count)
        {
            int count = Convert.ToInt32(Count);
            int totalProcessedCount = 0;

            List<Worker> workers = new List<Worker>();
            Random rnd = new Random();

            //Parallel.For method in C# is a blocking operation
            Parallel.For(0, count, i =>
            {
                Worker worker = new Worker();
                workers.Add(worker);

                Params param = new Params();
                string guid = System.Guid.NewGuid().ToString();
                param.Code = guid.Substring(0, 8);
                param.Count = rnd.Next(5, 20);

                Debug.WriteLine("Code:{0}, Count: {1}", param.Code, param.Count);
                worker.DoWork(param);

            });

            foreach (Worker worker in workers)
            {
                totalProcessedCount += worker.ProcessedCount;
            }

            Console.WriteLine("Total processed count: " + totalProcessedCount);
            Debug.WriteLine("Total processed count: " + totalProcessedCount);

            return totalProcessedCount.ToString();

        }


    }

}

public class Params
{
    public string Code { get; set; }
    public int Count { get; set; }
}

public class Worker
{
    public int ProcessedCount { get; private set; }

    public void DoWork(object data)
    {
        Params param = data as Params;
        string code = param.Code;
        int count = param.Count;

        for (int i = 0; i < count; i++)
        {
            ProcessedCount++;
            System.Threading.Thread.Sleep(500);       //0.5 seconds: synchronous, blocks the current thread
            Debug.WriteLine(code + ": " + ProcessedCount.ToString());
        }
    }
}

