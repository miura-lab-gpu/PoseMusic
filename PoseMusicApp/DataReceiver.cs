using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PoseMusicApp
{
	internal class DataReceiver
	{
		private Action<PoseData> udpCallback;
		private Action<Image> tcpCallback;
		
		public DataReceiver(Action<PoseData> udpCallback, Action<Image> tcpCallback) {
			this.udpCallback = udpCallback;
			this.tcpCallback = tcpCallback;

			Task.Run(() => UdpReceiver());
			Task.Run(() => TcpReceiver());
		}

		private void UdpReceiver()
		{
			UdpClient udp = new UdpClient(9001);

			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string json = null;

			while (true)
			{
				try
				{
					byte[] data = udp.Receive(ref ep);
					json = Encoding.UTF8.GetString(data);
					PoseData poseData = JsonConvert.DeserializeObject<PoseData>(json);
					udpCallback(poseData);
				} catch (SocketException e)
				{
					if (e.SocketErrorCode != SocketError.TimedOut) Console.WriteLine(e.Message);
				}
			}
		}

		private void TcpReceiver()
		{
			TcpClient tcp = new TcpClient();
			tcp.Connect("127.0.0.1", 9000);

			NetworkStream stream = tcp.GetStream();

			while (true)
			{
				byte[] sizeBuf = new byte[4];
				stream.Read(sizeBuf, 0, 4);
				int size = BitConverter.ToInt32(sizeBuf.Reverse().ToArray(), 0);

				byte[] imgBuf = new byte[size];
				int read = 0;
				while (read < size) read += stream.Read(imgBuf, read, size - read);

				var ms = new MemoryStream(imgBuf);
				Image img = Image.FromStream(ms);
				ms.Close();
				tcpCallback(img);
			}
		}
	}

	[Serializable]
	public class PoseData
	{
		public int frame_id;
		public long timestamp;

		public List<List<Landmark>> poses;
		public List<List<Gesture>> gestures;
		public List<List<Landmark>> hands;
	}

	[Serializable]
	public class Landmark
	{
		public float x;
		public float y;
		public float z;
	}

	[Serializable]
	public class Gesture
	{
		public string category_name;
		public float score;
	}
}
