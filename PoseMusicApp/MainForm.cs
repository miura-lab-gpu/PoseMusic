using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PoseMusicApp
{
	public partial class MainForm : Form
	{
		private DataReceiver receiver;
		private Process cameraPy;
		private DataWrapper dataWrapper;

		public MainForm()
		{
			InitializeComponent();

			StartCameraPy();
			
			receiver = new DataReceiver(UdpCallback, TcpCallback);
			dataWrapper = new DataWrapper();
		}

		private void StartCameraPy()
		{
			string root = new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.Parent.FullName;

			cameraPy = new Process();
			cameraPy.StartInfo.WorkingDirectory = root;
			cameraPy.StartInfo.FileName = "\"" + Path.Combine(root, "venv", "Scripts", "python.exe") + "\"";
			cameraPy.StartInfo.Arguments = "\"" + Path.Combine(root, "Pose", "camera.py") + "\"" + " --not_show_image"; //--not_print_debug_image";
			cameraPy.StartInfo.CreateNoWindow = true;
			cameraPy.StartInfo.UseShellExecute = false;
			Console.WriteLine(cameraPy.StartInfo.FileName);
			Console.WriteLine(cameraPy.StartInfo.Arguments);
			cameraPy.Start();
		}

		private void UdpCallback(PoseData data)
		{
			dataWrapper.InputData(data);
		}

		private void TcpCallback(Image img)
		{
			pictureBox.Invoke((MethodInvoker)(() => {
				var old = pictureBox.Image;
				pictureBox.Image = img;
				old?.Dispose();
			}));
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			try
			{
				cameraPy?.Kill();
			}
			catch (InvalidOperationException) { }
		}
	}
}
