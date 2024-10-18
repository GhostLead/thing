﻿using System.Collections.Generic; // Ensure to include necessary namespaces
using System.Net.Sockets;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace HackCuccos
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            StartUp();
            ExecuteClient();
        }
        static void ExecuteClient()
        {
            try
            {
                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);
                Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                bool ize = true;
                try
                {
                    sender.Connect(localEndPoint);
                    Console.WriteLine("Socket connected to -> {0} ", sender.RemoteEndPoint.ToString());
                    
                    // Loop for sending messages
                    while (ize)
                    {
                        Console.Write("Enter the message to send ('exit' to terminate): ");

                        byte[] messageSent = [];
                        foreach (var item in Finding.Detailsek)
                        {
                            messageSent.Append<Byte>(Byte.Parse($"\n{item.Value}"+"<EOF>"));

                        }

                        int byteSent = sender.Send(messageSent);

                        byte[] messageReceived = new byte[1024];
                        int byteRecv = sender.Receive(messageReceived);
                        Console.WriteLine("Message from Server -> {0}", Encoding.ASCII.GetString(messageReceived, 0, byteRecv));
                        ize = false;
                    }

                    // Close the socket
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private void ReadPdf(string filePath)
        {
            var findings = new List<Finding>();
            Finding currentFinding = null;
            string currentSection = null;

            using (PdfReader reader = new PdfReader(filePath))
            using (PdfDocument pdf = new PdfDocument(reader))
            {
                for (int page = 1; page <= pdf.GetNumberOfPages(); page++)
                {
                    var strategy = new LocationTextExtractionStrategy();
                    PdfCanvasProcessor parser = new PdfCanvasProcessor(strategy);
                    parser.ProcessPageContent(pdf.GetPage(page));
                    string text = strategy.GetResultantText();
                    var lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);

                    foreach (var line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (Regex.IsMatch(trimmedLine, @"^\d+ - .+"))
                        {
                            if (currentFinding != null)
                            {
                                findings.Add(currentFinding);
                            }
                            currentFinding = new Finding { Title = trimmedLine };
                            currentSection = null;
                        }
                        else if (currentFinding != null)
                        {
                            if (Regex.IsMatch(trimmedLine, @"^(Synopsis|Description|Solution|Risk Factor|See Also|Plugin Information|Plugin Output)$"))
                            {
                                currentSection = trimmedLine;
                                currentFinding.AddDetail(currentSection, "");
                            }
                            else if (!string.IsNullOrWhiteSpace(trimmedLine) && currentSection != null)
                            {
                                if (currentSection == "Risk Factor")
                                {
                                    // Set the risk factor for styling
                                    currentFinding.RiskFactor = trimmedLine;
                                }
                                currentFinding.AppendToDetail(currentSection, trimmedLine);
                            }
                        }
                    }
                }

                if (currentFinding != null)
                {
                    findings.Add(currentFinding);
                }
            }

            DisplayFindings(findings);
        }

        private void DisplayFindings(List<Finding> findings)
        {
            // Clear existing findings
            FindingsStackPanel.Children.Clear();

            // Create a ScrollViewer to allow scrolling
            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled // Disable horizontal scrolling unless needed
            };

            // Create a StackPanel inside the ScrollViewer
            StackPanel contentPanel = new StackPanel();

            // Add the findings to the StackPanel
            foreach (var finding in findings)
            {
                // Initialize default styles
                SolidColorBrush background = Brushes.Transparent;

                // Check if the "Risk Factor" section exists and determine background colors based on its content
                if (finding.Details.TryGetValue("Risk Factor", out var riskFactorContent))
                {
                    if (riskFactorContent.StartsWith("Critical", StringComparison.OrdinalIgnoreCase))
                    {
                        background = Brushes.Red;
                    }
                    else if (riskFactorContent.StartsWith("High", StringComparison.OrdinalIgnoreCase))
                    {
                        background = Brushes.Orange;
                    }
                    else if (riskFactorContent.StartsWith("Medium", StringComparison.OrdinalIgnoreCase))
                    {
                        background = Brushes.Yellow;
                    }
                    else if (riskFactorContent.StartsWith("Low", StringComparison.OrdinalIgnoreCase))
                    {
                        background = Brushes.LightGreen;
                    }
                    else if (riskFactorContent.StartsWith("None", StringComparison.OrdinalIgnoreCase))
                    {
                        background = Brushes.DodgerBlue;
                    }
                }

                var expander = new Expander
                {
                    Header = new TextBlock
                    {
                        Text = finding.Title,
                        TextWrapping = TextWrapping.Wrap,
                        Background = background,
                        Foreground = Brushes.Black,
                        Padding = new Thickness(5)
                    },
                    IsExpanded = false,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var detailsPanel = new StackPanel();

                foreach (var detail in finding.Details)
                {
                    detailsPanel.Children.Add(new TextBlock
                    {
                        Text = detail.Key,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Black,
                        Margin = new Thickness(0, 10, 0, 5)
                    });
                    detailsPanel.Children.Add(new TextBlock
                    {
                        Text = detail.Value,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.Black
                    });
                }

                expander.Content = detailsPanel;
                contentPanel.Children.Add(expander);
            }

            // Add StackPanel with content to the ScrollViewer
            //scrollViewer.Content = contentPanel;

            // Finally, add the ScrollViewer to the main layout
            FindingsStackPanel.Children.Add(contentPanel);
        }


        private Border CreateHeader(Finding finding)
        {
            var headerBorder = new Border
            {
                Margin = new Thickness(0, 0, 0, 5), // Add margin for spacing
                Padding = new Thickness(5) // Add padding for aesthetics
            };

            var headerText = new TextBlock
            {
                Text = finding.Title,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Black // Set foreground to black
            };

            // Set colors based on the risk factor
            switch (finding.RiskFactor)
            {
                case var risk when risk.StartsWith("Critical"):
                    headerBorder.Background = Brushes.Red;
                    headerText.Foreground = Brushes.White; // Changed to white for better contrast
                    break;
                case var risk when risk.StartsWith("High"):
                    headerBorder.Background = Brushes.Orange;
                    headerText.Foreground = Brushes.White; // Changed to white for better contrast
                    break;
                case var risk when risk.StartsWith("Medium"):
                    headerBorder.Background = Brushes.Yellow;
                    headerText.Foreground = Brushes.Black; // Changed to black for better contrast
                    break;
                case var risk when risk.StartsWith("Low"):
                    headerBorder.Background = Brushes.LightGreen;
                    headerText.Foreground = Brushes.Black; // Changed to black for better contrast
                    break;
                case var risk when risk.StartsWith("Info"):
                    headerBorder.Background = Brushes.Blue;
                    headerText.Foreground = Brushes.White; // Changed to white for better contrast
                    break;
                default:
                    headerBorder.Background = Brushes.Gray; // Default case for unexpected risk factors
                    headerText.Foreground = Brushes.White; // Changed to white for better contrast
                    break;
            }

            headerBorder.Child = headerText; // Set the header text as the child of the border
            return headerBorder; // Return the border
        }

        private void StartUp()
        {
            string filePath = "..\\..\\..\\SampleNetworkVulnerabilityScanReport.pdf";
            ReadPdf(filePath);
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ShutDownButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    public class Finding
    {
        public string Title { get; set; }
        public Dictionary<string, string> Details { get; set; } = new Dictionary<string, string>();
        public static Dictionary<string, string> Detailsek { get; set; } = new Dictionary<string, string>();
        public string RiskFactor { get; set; } // New property to hold risk factor

        public void AddDetail(string key, string value)
        {
            Details[key] = value;
            Detailsek[key] = value;
        }

        public void AppendToDetail(string key, string text)
        {
            if (Details.ContainsKey(key))
            {
                Details[key] += Details[key].Length > 0 ? " " + text : text;
            }
            else
            {
                Details[key] = text;
            }
            
            if (Detailsek.ContainsKey(key))
            {
                Detailsek[key] += Detailsek[key].Length > 0 ? " " + text : text;
            }
            else
            {
                Detailsek[key] = text;
            }
        }
    }
}
