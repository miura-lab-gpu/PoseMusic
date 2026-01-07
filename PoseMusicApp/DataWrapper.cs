using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoseMusicApp
{
	internal class DataWrapper
	{
		private PoseData currentData;
		private PoseData velocityData;

		public DataWrapper() { }

		public void InputData(PoseData data)
		{
			velocityData = data.GetVelocityData(currentData);
			currentData = data;
			if (velocityData == null) return;
			Console.WriteLine(JsonConvert.SerializeObject(velocityData.poses[0][0]));
		}
	}
}
