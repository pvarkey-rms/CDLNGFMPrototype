using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RMS.NGFMAutomation.LoopEvents;
using System.IO;
using System.Data.SqlClient;
using System.Xml;


namespace RMS.NGFMAutomation.LoopEvents
{
    public partial class Form1 : Form
    {
        public RMS.NGFMAutomation.LoopEvents.NGFMAutomation automation { get; set; }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XLSX|*.xlsx";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = ofd.FileName;
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = fbd.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                textBox7.Text = fbd.SelectedPath;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            
            string InputXlPath = textBox1.Text;
            string OutputXlPath = textBox2.Text;
            bool RunAllTestCases = false;
            if (RunAllCases.Checked)
                RunAllTestCases = true;

            if (InputXlPath == "")
            {
                MessageBox.Show("Please provide Input Excel Path");
                return;
            }
            else if(File.Exists(InputXlPath)==false)
            {
                MessageBox.Show("Can not find Input Excel. Please enter a valid Input Excel Path");
                return;
            }

            if(OutputXlPath =="")
            {
                MessageBox.Show("Please provide Output Excel Path");
                return;
            }
            else if (Directory.Exists(OutputXlPath) == false)
            {
                MessageBox.Show("Can not find Output Excel folder. Please enter a valid Output Excel folder");
                return;
            }

            OutputObject outputObject = new OutputObject();

            if (textBox3.Text == "")
            {
                MessageBox.Show("Please provide Output SQL Server Name");
                return;
            }
            outputObject.OutPutServerName = textBox3.Text;
            if (textBox4.Text == "")
            {
                MessageBox.Show("Please provide Output SQL Login");
                return;
            }
            outputObject.OutPutUserName = textBox4.Text;
            if (textBox5.Text == "")
            {
                MessageBox.Show("Please provide Output SQL Password");
                return;
            }
            outputObject.OutPutPass = textBox5.Text;
            if (textBox6.Text == "")
            {
                MessageBox.Show("Please provide Output Database");
                return;
            }
            outputObject.OutPutDataBase = textBox6.Text;

            SqlConnection conn = new SqlConnection();
            try
            {
                conn.ConnectionString = "Server=" + outputObject.OutPutServerName + ";Database=" + outputObject.OutPutDataBase + ";User Id=" + outputObject.OutPutUserName + ";Password=" + outputObject.OutPutPass;
                conn.Open();
            }
            catch (SqlException E)
            {
                conn.Close();
                MessageBox.Show("Invalid Output SQL Connection. Please check your SQL info. \n Error :"+ E.Message);
                return;
            }
            finally
            {
                conn.Close(); 
            }

            automation = new RMS.NGFMAutomation.LoopEvents.NGFMAutomation(InputXlPath, OutputXlPath,outputObject);
            try
            {
                automation.Run(RunAllTestCases);
            }
            catch(InvalidDataException invalidE)
            {
                MessageBox.Show(invalidE.Message);
                return;
            }
            MessageBox.Show("Automation Run Completed! Please check Output Excel and SQL Database for results.");
            Application.Exit();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string InputXlPath = textBox1.Text;
            string ExtractOutputFolder = textBox7.Text;
            bool RunAllTestCases = false;
            if (RunAllCases.Checked)
                RunAllTestCases = true;
   
            if (textBox7.Text == "")
            {
                MessageBox.Show("To genearte EDS Extract, please provide a path for Extract Output Path.");
                return;
            }
            else if (Directory.Exists(ExtractOutputFolder) == false)
            {
                MessageBox.Show("Can not find Extract Output Path. Please enter a valid Input Excel Path");
                return;
            }
            if (InputXlPath == "")
            {
                MessageBox.Show("Please provide Input Excel Path");
                return;
            }
            else if (File.Exists(InputXlPath) == false)
            {
                MessageBox.Show("Can not find Input Excel. Please enter a valid Input Excel Path");
                return;
            }

            //string xmlFilePath = @"..\Sandbox\CDLNGFMPrototype\RMS.Prototype.NGFMTestAutomation\EDS_Extract_API\Config.xml";
            string xmlFilePath = @".\Config.xml";
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(xmlFilePath);

            foreach (XmlNode childnode in xmldoc.ChildNodes)
            {
                if (childnode.Name.Equals("Configuration"))
                {
                    foreach (XmlNode child in childnode.ChildNodes)
                    {
                        if (child.Name.Equals("EDSExtractTargetFolder"))
                        {
                            child.InnerText = ExtractOutputFolder+"\\";
                            xmldoc.Save(xmlFilePath);
                            break;
                        }
                    }
                }
            }
            automation = new RMS.NGFMAutomation.LoopEvents.NGFMAutomation(InputXlPath, textBox8);
            automation.GenerateAllEDSExtract(RunAllTestCases);
            Application.Exit();
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox1.Text=@"D:\Nina_Automation_Testing\InputSheet\NTA Test Case Input Nina Testing_LoopThroughEvents.xlsx";
            textBox2.Text=@"D:\Nina_Automation_Testing\OutputSheet";
            textBox3.Text="ca1modelcert03";
            textBox4.Text="sa";
            textBox5.Text="Rmsuser!";
            textBox6.Text="NTA_Comparison";
        }
    }
}
