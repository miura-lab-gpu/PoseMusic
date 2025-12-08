using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PoseMusicApp
{
	internal static class Program
	{
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main()
		{
			var generator = new RandomMusicGenerator();

			// 明るくエネルギッシュな曲（和音・ベースあり）
			var happyParams = new MusicParameters
			{
				Tempo = 130,
				Mood = MusicMood.Happy,
				ScaleType = Scale.Major,
				DurationSeconds = 2,
				Volume = 0.1f,
				EnableHarmony = true,
				EnableBass = true,
				PatternType = MelodyPattern.Balanced,
				HarmonyPatternType = HarmonyPattern.Arpeggiated
			};

			// 悲しい曲（順次進行中心、パッド和音）
			var sadParams = new MusicParameters
			{
				Tempo = 75,
				Mood = MusicMood.Sad,
				ScaleType = Scale.Minor,
				Volume = 0.1f,
				EnableHarmony = true,
				EnableBass = true,
				PatternType = MelodyPattern.Stepwise,
				HarmonyPatternType = HarmonyPattern.Pad
			};

			// 穏やかな曲（アルペジオメロディー、サステイン和音）
			var calmParams = new MusicParameters
			{
				Tempo = 60,
				Mood = MusicMood.Calm,
				ScaleType = Scale.Pentatonic,
				Volume = 0.1f,
				EnableHarmony = true,
				EnableBass = false,
				PatternType = MelodyPattern.Arpeggiated,
				HarmonyPatternType = HarmonyPattern.Sustained
			};

			// エネルギッシュな曲（跳躍進行、パルス和音）
			var energeticParams = new MusicParameters
			{
				Tempo = 150,
				Mood = MusicMood.Energetic,
				ScaleType = Scale.Major,
				Volume = 0.1f,
				EnableHarmony = true,
				EnableBass = true,
				PatternType = MelodyPattern.Jumping,
				HarmonyPatternType = HarmonyPattern.Pulsing
			};

			// ミニマルな曲（トレモロ和音）
			var minimalParams = new MusicParameters
			{
				Tempo = 90,
				Mood = MusicMood.Mysterious,
				ScaleType = Scale.Minor,
				Volume = 0.1f,
				EnableHarmony = true,
				EnableBass = true,
				PatternType = MelodyPattern.Minimal,
				HarmonyPatternType = HarmonyPattern.Tremolo
			};

			// リズミックな曲
			var rhythmicParams = new MusicParameters
			{
				Tempo = 120,
				Mood = MusicMood.Happy,
				ScaleType = Scale.Major,
				Volume = 0.1f,
				EnableHarmony = true,
				EnableBass = true,
				PatternType = MelodyPattern.Syncopated,
				HarmonyPatternType = HarmonyPattern.Rhythmic
			};

			// リアルタイム再生のデモ
			Console.WriteLine("\n=== Dynamic Real-time Playback Demo ===");
			Console.WriteLine("明るい曲");
			generator.StartDynamicPlayback(happyParams);
			Thread.Sleep(10000);
			Console.WriteLine("悲しい曲");
			generator.UpdateParameters(sadParams);
			Thread.Sleep(10000);
			Console.WriteLine("穏やかな曲");
			generator.UpdateParameters(calmParams);
			Thread.Sleep(10000);
			Console.WriteLine("エネルギッシュな曲");
			generator.UpdateParameters(energeticParams);
			Thread.Sleep(10000);
			Console.WriteLine("ミニマルな曲");
			generator.UpdateParameters(minimalParams);
			Thread.Sleep(10000);
			Console.WriteLine("リズミックな曲");
			generator.UpdateParameters(rhythmicParams);
			Thread.Sleep(10000);

			// 停止
			generator.StopPlayback();
			Console.WriteLine("Playback stopped.");



			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}
