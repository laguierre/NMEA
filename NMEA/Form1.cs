using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NMEA
{
    public partial class Form1 : Form
    {
        public string NMEA_HDT { get; private set; }

        public class Globals
        {
            public static string hdtValue = "000.0";
            public static string rllValue = "00.00,B,";
            public static string pthValue = "00.00,M";
            public static string hdtNMEA;                   //Trama HPHDT NMEA (Heading)
            public static string troNMEA;                   //Trama HPTRO NMEA (Pitch and Roll)
            public static string rllNMEA;                   //Trama propietario PRRLL (Roll)
            public static string pthNMEA;                   //Trama propietario PRPTH (Pitch)
            public const string NMEA_HDT = "$HEHDT,";
            public const string NMEA_TRO = "$PHTRO,";      
            public const string NMEA_RLL = "$PRRLL,";       //$PRRLL,SXX.XX*hh<CR><LF>
            public const string NMEA_PTH = "$PRPTH,";       //$PRPTH,SXX.XX*hh<CR><LF>

            public static string HdtValue { get => hdtValue; set => hdtValue = value; }
        }
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            String[] ports = System.IO.Ports.SerialPort.GetPortNames();

            Globals.hdtNMEA = nmeaHDTSet(Globals.HdtValue);         //Initial Heading
            Globals.troNMEA = nmeaRnPSet();                         //Iinitial Pitch and Roll
            Globals.pthNMEA = nmeaPTHSet("+00.00");                 //Initial Pitch
            Globals.rllNMEA = nmeaRLLSet("+00.00");                 //Initial Roll

            statusStrip1.Items[1].Text = Globals.troNMEA;
            statusStrip1.Items[0].Text = Globals.hdtNMEA;
            statusStrip1.Items[2].Text = Globals.rllNMEA;
            statusStrip1.Items[3].Text = Globals.pthNMEA;

            btnCloseCOMM.Enabled = false;
            btnOpenCOMM.Enabled = false;
            label1.Text = "Heading [$xxHDT]: 0,0°";
            try
            {
                if (ports.Length > 0)
                {
                    cboPort.Items.AddRange(ports);
                    cboPort.SelectedIndex = 0;
                    btnOpenCOMM.Enabled = true;
                    serialPort.WriteTimeout = 500;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ocurrió un problema con el puerto", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /**
         *  @brief: Botón Cerrado de puerto
         *  
         */
        private void button1_Click(object sender, EventArgs e)
        {
            btnCloseCOMM.Enabled = true;
            try
            {
                if (cboPort.Text != null)
                {
                    serialPort.PortName = cboPort.Text;
                    serialPort.Open();
                    trackBar1.Enabled = true;
                    trackBar2.Enabled = true;
                    trackBar3.Enabled = true;
                    btnOpenCOMM.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /**
         * @brief: Botón Cerrado de puerto
         * 
         */
        private void button2_Click(object sender, EventArgs e)
        {
            btnCloseCOMM.Enabled = false;
            btnOpenCOMM.Enabled = true;
            try
            {
                serialPort.Close();
                trackBar1.Enabled = false;
                trackBar2.Enabled = false;
                trackBar3.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cboPort_DropDown(object sender, EventArgs e)
        {
            String[] ports = System.IO.Ports.SerialPort.GetPortNames();
            cboPort.Items.Clear();
            cboPort.Items.AddRange(ports);

            //            byte[] mBuffer = Encoding.ASCII.GetBytes("90");

        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (serialPort.IsOpen) {
                serialPort.Close(); 
                serialPort.Dispose(); 
            }
        }
        /**
         * brief: Heading Slider
         */
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            float value = trackBar1.Value;
            value /= 10;
            Globals.HdtValue = String.Format("{0:000.0}", value).Replace(',', '.');
            label1.Text = "Heading [$xxHDT]: " + Globals.HdtValue + '°';
            Globals.hdtNMEA = nmeaHDTSet(Globals.HdtValue);
            statusStrip1.Items[0].Text = Globals.hdtNMEA;
            TxCOMM(Globals.hdtNMEA.ToCharArray());
        }
        /**
         * brief: Roll Slider
         * 
         */
        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            float value = trackBar2.Value;
            string babor, sign;
            value /= 100;
            label2.Text = "Roll [$xxPHTRO]: " + String.Format("{0:0.00}", value) + '°';
            if (value > 0)
            {
                babor = ",B,";             //PROA up
                sign = "+";
            }
            else
            {
                babor = ",T,";             //PROA down
                sign = "-";
                value = value * -1;
            }
            Globals.rllValue = String.Format("{0:00.00}", value).Replace(',', '.');
            Globals.rllNMEA = nmeaRLLSet(sign + Globals.rllValue);
            Globals.rllValue += babor;
            Globals.troNMEA = nmeaRnPSet();
           
            statusStrip1.Items[1].Text = Globals.troNMEA;
            statusStrip1.Items[2].Text = Globals.rllNMEA;
            if (checkBox1.Checked)
            {
                TxCOMM(Globals.troNMEA.ToCharArray());
            }
            else
            {
                TxCOMM(Globals.rllNMEA.ToCharArray());
            }
        }
        /**
         * brief: Pitch Slider
         * 
         */
        private void trackBar3_ValueChanged(object sender, EventArgs e)
        {
            float value = trackBar3.Value;
            string proa, sign;
            value /= 100;
            label3.Text = "Pitch [$xxPHTRO]: " + String.Format("{0:0.00}", value) + '°';
            if (value > 0)
            {
                proa = ",M";             //PROA up
                sign = "+";
            }
            else
            {
                proa = ",P";             //PROA down
                value = value * -1; 
                sign = "-";
            }
            Globals.pthValue = String.Format("{0:00.00}", value).Replace(',', '.');
            Globals.pthNMEA = nmeaPTHSet(sign + Globals.pthValue);
            Globals.pthNMEA += proa;
            Globals.troNMEA = nmeaRnPSet();
            statusStrip1.Items[1].Text = Globals.troNMEA;          
            statusStrip1.Items[3].Text = Globals.pthNMEA;
            if (checkBox1.Checked)
            {
                TxCOMM(Globals.troNMEA.ToCharArray());
            }
            else
            {
                TxCOMM(Globals.pthNMEA.ToCharArray());
            }

        }
        String nmeaHDTSet(String angle) {
            String HDT = Globals.NMEA_HDT + angle + ",T*";
            String chkSum = getChecksum(HDT);
            return (HDT + chkSum + "\r\n");
        }
        String nmeaRLLSet(String angle)
        {
            String RLL = Globals.NMEA_RLL + angle + "*";
            String chkSum = getChecksum(RLL);
            return (RLL + chkSum + "\r\n");
        }
        String nmeaPTHSet(String angle)
        {
            String PTH = Globals.NMEA_PTH + angle + "*";
            String chkSum = getChecksum(PTH);
            return (PTH + chkSum + "\r\n");
        }
        String nmeaRnPSet()
        {
            String PHTRO = Globals.NMEA_TRO + Globals.rllValue + Globals.pthValue + "*";
            String chkSum = getChecksum(PHTRO);
            return (PHTRO + chkSum + "\r\n");
        }
        // Calculates the checksum for a sentence
        static string getChecksum(string sentence)
        {
            //Start with first Item
            int checksum = Convert.ToByte(sentence[sentence.IndexOf('$') + 1]);
            // Loop through all chars to get a checksum
            for (int i = sentence.IndexOf('$') + 2; i < sentence.IndexOf('*'); i++)
            {
                // No. XOR the checksum with this character's value
                checksum ^= Convert.ToByte(sentence[i]);
            }
            // Return the checksum formatted as a two-character hexadecimal
            return checksum.ToString("X2");
        }

        private void cboPort_TextChanged(object sender, EventArgs e)
        {
            btnOpenCOMM.Enabled = true;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            checkBox3.Checked = !checkBox2.Checked;
            if (checkBox2.Checked)
                timer1.Enabled = false;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            checkBox2.Checked = !checkBox3.Checked;
            if (checkBox3.Checked) {
                if (numericUpDown1.Value < 100)
                    numericUpDown1.Value = 100;
                else if (numericUpDown1.Value > 9999)
                {
                    numericUpDown1.Value = 9999;
                }
                else
                {
                    timer1.Interval = Decimal.ToInt32(numericUpDown1.Value);
                    timer1.Enabled = true;
                }
            }

        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }
        int TxCOMM(char[] Tx) {
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Write(Tx, 0, Tx.Length);
                    /*TODO: queraría definir el END OF TRANSMITION*/
                    return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ocurrió un problema con el puerto", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    if (checkBox1.Checked)
                    {                
                        TxCOMM(Globals.hdtNMEA.ToCharArray());
                        TxCOMM(Globals.troNMEA.ToCharArray());
                    }
                    else
                    {
                        TxCOMM(Globals.hdtNMEA.ToCharArray());
                        TxCOMM(Globals.pthNMEA.ToCharArray());
                        TxCOMM(Globals.rllNMEA.ToCharArray());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ocurrió un problema con el puerto", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!serialPort.IsOpen) {
                serialPort.BaudRate = Int32.Parse(comboBox2.Text);
            }
            else
                MessageBox.Show("Advertencia", "Debe cerrar el puerto", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
