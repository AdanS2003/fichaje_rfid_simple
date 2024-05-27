using MySql.Data.MySqlClient;
using System.Text;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Moodle_db_connect
{
    public partial class Form1 : Form
    {
        StringBuilder inputBuffer = new StringBuilder();

        private System.Timers.Timer keyPressTimer = new System.Timers.Timer();
        private DateTime lastKeyPressTime;
        private const int TimeThresholdMilliseconds = 40; // Time threshold in milliseconds, adjust as needed

        public Form1()
        {
            InitializeComponent();
            textBox1.Focus();
            this.KeyPreview = true; // Allow the form to receive key events first
            this.KeyDown += new KeyEventHandler(Form1_KeyDown); // Subscribe to the KeyDown event
            this.KeyPress += new KeyPressEventHandler(Form1_KeyPress); // Subscribe to the KeyPress event
            // Timer setup
            keyPressTimer.Interval = TimeThresholdMilliseconds;
            keyPressTimer.Elapsed += KeyPressTimer_Elapsed;
            keyPressTimer.AutoReset = false; // Ensures the timer only runs once per keypress sequence

            lastKeyPressTime = DateTime.Now;

        }

        private void KeyPressTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Clears the input buffer if the timer elapses
            inputBuffer.Clear();
        }


        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string nfcData = inputBuffer.ToString();
                textBox1.Text = nfcData; // Display the NFC data in textBox1
                textBox1.ReadOnly = true; // Disable writing to the TextBox
                labelError.ForeColor = Color.Green;
                labelError.Text = "Registro de acceso creado correctamente para davidbrelop";
                // queryMoodle(nfcData); // Call the method with NFC data

                inputBuffer.Clear(); // Clear the buffer for the next input
                e.Handled = true; // Mark the event as handled
            }
        }


        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Calculate the time since the last key press
            TimeSpan timeSinceLastKeyPress = DateTime.Now - lastKeyPressTime;

            // Reset the timer and the timestamp
            lastKeyPressTime = DateTime.Now;
            keyPressTimer.Stop();
            keyPressTimer.Start();

            // If the time since the last keypress is below the threshold, append the character
            if (timeSinceLastKeyPress.TotalMilliseconds < TimeThresholdMilliseconds)
            {
                inputBuffer.Append(e.KeyChar);
            }
            else
            {
                // If too much time has passed, clear the buffer before appending
                inputBuffer.Clear();
                inputBuffer.Append(e.KeyChar);
            }

            e.Handled = true;
        }

        private async void queryMoodle(string nfcData)
        {
            string apiEndpoint = "http://172.17.127.254:8200/webservice/rest/create_new_access_log_with_nfc_token.php";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string, string>("token", nfcData)
            });

                    HttpResponseMessage response = await client.PostAsync(apiEndpoint, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(apiResponse);
                        XmlNode estadoNode = xmlDoc.SelectSingleNode("/respuesta/estado");
                        string status = estadoNode?.InnerText ?? "Respuesta Desconocida";

                        if (status.ToLower() == "error")
                        {
                            XmlNode messageNode = xmlDoc.SelectSingleNode("/respuesta/mensaje");
                            string errorMessage = messageNode?.InnerText ?? "Error Desconocido";
                            labelError.ForeColor = Color.Red;
                            labelError.Text = "Error: " + errorMessage;
                        }
                        else
                        {
                            XmlNode usernameNode = xmlDoc.SelectSingleNode("/respuesta/nombre_usuario");
                            if (usernameNode != null)
                            {
                                string username = usernameNode.InnerText;
                                labelError.ForeColor = Color.Green;
                                labelError.Text = "Registro de acceso creado correctamente para " + username;
                                UpdateAttendance(username);
                            }
                            else
                            {
                                labelError.Text = "Registro de acceso creado correctamente pero no se pudo obtener el nombre de usuario.";
                            }
                        }
                    }
                    else
                    {
                        labelError.ForeColor = Color.Red;
                        labelError.Text = "Error HTTP: " + response.StatusCode.ToString();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error: " + e.Message);
                }
            }
        }
        private async void UpdateAttendance(string username)
        {
            string apiEndpoint = "http://172.17.127.254:8200/webservice/rest/fill_attendance_by_username.php"; // Adjust the URL to your actual API endpoint

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string, string>("username", username)
            });

                    // Sending a POST request
                    HttpResponseMessage response = await client.PostAsync(apiEndpoint, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        MessageBox.Show("Attendance Updated: " + apiResponse, "Update Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to update attendance: HTTP Error " + response.StatusCode.ToString(), "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error updating attendance: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

    }
}
