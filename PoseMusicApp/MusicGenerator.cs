using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PoseMusicApp
{
	/// <summary>
	/// 音楽生成のパラメーター
	/// </summary>
	public class MusicParameters
	{
		public int Tempo { get; set; } = 120; // BPM
		public MusicMood Mood { get; set; } = MusicMood.Neutral;
		public Scale ScaleType { get; set; } = Scale.Major;
		public int DurationSeconds { get; set; } = 30;
		public float Volume { get; set; } = 0.2f; // 破裂防止のため低めに設定
		public bool EnableHarmony { get; set; } = true; // 和音を有効化
		public bool EnableBass { get; set; } = true; // ベースラインを有効化
		public MelodyPattern PatternType { get; set; } = MelodyPattern.Balanced; // メロディーパターン
		public HarmonyPattern HarmonyPatternType { get; set; } = HarmonyPattern.Block; // 和音パターン

		/// <summary>
		/// パラメーターのディープコピーを作成
		/// </summary>
		public MusicParameters Clone()
		{
			return new MusicParameters
			{
				Tempo = this.Tempo,
				Mood = this.Mood,
				ScaleType = this.ScaleType,
				DurationSeconds = this.DurationSeconds,
				Volume = this.Volume,
				EnableHarmony = this.EnableHarmony,
				EnableBass = this.EnableBass,
				PatternType = this.PatternType,
				HarmonyPatternType = this.HarmonyPatternType
			};
		}
	}

	/// <summary>
	/// パラメーター変更ポイント
	/// </summary>
	public class ParameterChange
	{
		public double TimeSeconds { get; set; } // 変更タイミング（秒）
		public MusicParameters Parameters { get; set; } // 新しいパラメーター
		public double TransitionDuration { get; set; } = 2.0; // トランジション時間（秒）
	}

	/// <summary>
	/// 音楽の雰囲気
	/// </summary>
	public enum MusicMood
	{
		Happy,      // 明るい
		Sad,        // 悲しい
		Energetic,  // エネルギッシュ
		Calm,       // 穏やか
		Mysterious, // 神秘的
		Neutral     // ニュートラル
	}

	/// <summary>
	/// 音階
	/// </summary>
	public enum Scale
	{
		Major,      // 長調
		Minor,      // 短調
		Pentatonic, // ペンタトニック
		Blues       // ブルース
	}

	/// <summary>
	/// メロディーパターン
	/// </summary>
	public enum MelodyPattern
	{
		Stepwise,        // 順次進行中心（滑らか）
		Jumping,         // 跳躍進行中心（ダイナミック）
		Arpeggiated,     // アルペジオ風（和音分散）
		Repetitive,      // 反復的（同じ音の繰り返し多め）
		ScaleRun,        // スケール走句（上昇・下降）
		Syncopated,      // シンコペーション（リズミカル）
		Sequence,        // シーケンス（モチーフの移調）
		Balanced,        // バランス型（様々な要素を組み合わせ）
		Minimal,         // ミニマル（少ない音での反復）
		Ornamental       // 装飾的（細かい音符多め）
	}

	/// <summary>
	/// 和音パターン
	/// </summary>
	public enum HarmonyPattern
	{
		Block,           // ブロックコード（全音同時）
		Arpeggiated,     // アルペジオ（分散和音）
		Pulsing,         // パルス（リズミカルな反復）
		Sustained,       // サステイン（長く伸ばす）
		Staccato,        // スタッカート（短く切る）
		RollingChord,    // ローリング（音を少しずつずらす）
		Alternating,     // 交互（高音と低音を交互に）
		Tremolo,         // トレモロ（素早い反復）
		Pad,             // パッド（柔らかく持続）
		Rhythmic         // リズミック（複雑なリズムパターン）
	}

	/// <summary>
	/// コード進行
	/// </summary>
	public class ChordProgression
	{
		public List<Chord> Chords { get; set; }
	}

	/// <summary>
	/// コード
	/// </summary>
	public class Chord
	{
		public List<int> Intervals { get; set; } // 基音からの半音数
		public string Name { get; set; }
	}

	/// <summary>
	/// ランダム音楽生成クラス
	/// </summary>
	public class RandomMusicGenerator
	{
		private readonly Random _random;
		private readonly int _sampleRate = 44100;
		private const float MaxAmplitude = 0.95f; // クリッピング防止
		private DynamicMusicProvider _dynamicProvider;
		private WaveOutEvent _waveOut;

		public RandomMusicGenerator(int? seed = null)
		{
			_random = seed.HasValue ? new Random(seed.Value) : new Random();
		}

		/// <summary>
		/// 音楽を生成してWAVファイルに保存
		/// </summary>
		public void GenerateAndSave(string filePath, MusicParameters parameters)
		{
			var provider = GenerateMusicProvider(parameters, null);

			// クリッピング防止のためのリミッター適用
			var limiter = new SoftLimiter(provider, MaxAmplitude);

			WaveFileWriter.CreateWaveFile16(filePath, limiter);
		}

		/// <summary>
		/// パラメーター変更を含む音楽を生成してWAVファイルに保存
		/// </summary>
		public void GenerateAndSaveWithChanges(string filePath, MusicParameters initialParameters,
			List<ParameterChange> changes)
		{
			var provider = GenerateMusicProvider(initialParameters, changes);

			// クリッピング防止のためのリミッター適用
			var limiter = new SoftLimiter(provider, MaxAmplitude);

			WaveFileWriter.CreateWaveFile16(filePath, limiter);
		}

		/// <summary>
		/// リアルタイムでパラメーター変更可能な音楽を再生開始
		/// </summary>
		public void StartDynamicPlayback(MusicParameters initialParameters)
		{
			if (_waveOut != null)
			{
				StopPlayback();
			}

			_dynamicProvider = new DynamicMusicProvider(this, _sampleRate, initialParameters);
			var limiter = new SoftLimiter(_dynamicProvider, MaxAmplitude);

			_waveOut = new WaveOutEvent();
			_waveOut.Init(limiter);
			_waveOut.Play();
		}

		/// <summary>
		/// 再生中のパラメーターを変更（シームレスに反映）
		/// </summary>
		public void UpdateParameters(MusicParameters newParameters, double transitionSeconds = 2.0)
		{
			if (_dynamicProvider != null)
			{
				_dynamicProvider.UpdateParameters(newParameters, transitionSeconds);
			}
		}

		/// <summary>
		/// 再生を停止
		/// </summary>
		public void StopPlayback()
		{
			if (_waveOut != null)
			{
				_waveOut.Stop();
				_waveOut.Dispose();
				_waveOut = null;
			}

			if (_dynamicProvider != null)
			{
				_dynamicProvider.Dispose();
				_dynamicProvider = null;
			}
		}

		/// <summary>
		/// 再生中かどうか
		/// </summary>
		public bool IsPlaying
		{
			get { return _waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing; }
		}

		/// <summary>
		/// 現在の再生時間（秒）
		/// </summary>
		public double CurrentTime
		{
			get { return _dynamicProvider != null ? _dynamicProvider.CurrentTimeSeconds : 0; }
		}

		/// <summary>
		/// 音楽を生成して再生
		/// </summary>
		public void GenerateAndPlay(MusicParameters parameters)
		{
			var provider = GenerateMusicProvider(parameters, null);
			var limiter = new SoftLimiter(provider, MaxAmplitude);

			using (var waveOut = new WaveOutEvent())
			{
				waveOut.Init(limiter);
				waveOut.Play();
				while (waveOut.PlaybackState == PlaybackState.Playing)
				{
					System.Threading.Thread.Sleep(100);
				}
			}
		}

		/// <summary>
		/// パラメーター変更を含む音楽を生成して再生
		/// </summary>
		public void GenerateAndPlayWithChanges(MusicParameters initialParameters,
			List<ParameterChange> changes)
		{
			var provider = GenerateMusicProvider(initialParameters, changes);
			var limiter = new SoftLimiter(provider, MaxAmplitude);

			using (var waveOut = new WaveOutEvent())
			{
				waveOut.Init(limiter);
				waveOut.Play();
				while (waveOut.PlaybackState == PlaybackState.Playing)
				{
					System.Threading.Thread.Sleep(100);
				}
			}
		}

		// 内部メソッド用のアクセサ
		internal Random Random { get { return _random; } }
		internal int SampleRate { get { return _sampleRate; } }

		/// <summary>
		/// 音楽プロバイダーを生成
		/// </summary>
		private ISampleProvider GenerateMusicProvider(MusicParameters parameters, List<ParameterChange> parameterChanges)
		{
			double beatDuration = 60.0 / parameters.Tempo;
			var chordProgression = GenerateChordProgression(parameters);
			var scaleNotes = GetScaleNotes(parameters.ScaleType, parameters.Mood);

			// すべてのトラックを同じ長さに統一するためのリスト
			var allProviders = new List<ISampleProvider>();

			// パラメーター変更がない場合は通常の生成
			if (parameterChanges == null || parameterChanges.Count == 0)
			{
				allProviders.AddRange(GenerateMelody(scaleNotes, parameters, beatDuration, chordProgression));

				if (parameters.EnableHarmony)
				{
					allProviders.AddRange(GenerateHarmony(chordProgression, parameters, beatDuration));
				}

				if (parameters.EnableBass)
				{
					allProviders.AddRange(GenerateBass(chordProgression, parameters, beatDuration));
				}
			}
			else
			{
				// パラメーター変更ありの場合はセクションごとに生成
				allProviders.AddRange(GenerateMusicWithParameterChanges(parameters, parameterChanges));
			}

			// すべてのプロバイダーを最大長に統一
			var maxDuration = TimeSpan.FromSeconds(parameters.DurationSeconds);
			var paddedProviders = allProviders.Select(p => new PaddedProvider(p, maxDuration)).ToList();

			// ミキサーに追加
			var mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(_sampleRate, 1))
			{
				ReadFully = true
			};

			foreach (var provider in paddedProviders)
			{
				mixer.AddMixerInput(provider);
			}

			return mixer.Take(maxDuration);
		}

		/// <summary>
		/// パラメーター変更を含む音楽を生成
		/// </summary>
		private List<ISampleProvider> GenerateMusicWithParameterChanges(MusicParameters initialParams,
			List<ParameterChange> changes)
		{
			var providers = new List<ISampleProvider>();
			var sortedChanges = changes.OrderBy(c => c.TimeSeconds).ToList();

			double currentTime = 0;
			var currentParams = initialParams.Clone();

			for (int i = 0; i < sortedChanges.Count; i++)
			{
				var change = sortedChanges[i];
				double sectionEnd = change.TimeSeconds;

				// 現在のパラメーターでセクションを生成
				if (sectionEnd > currentTime)
				{
					var sectionParams = currentParams.Clone();
					sectionParams.DurationSeconds = (int)(sectionEnd - currentTime);

					var beatDuration = 60.0 / sectionParams.Tempo;
					var chordProgression = GenerateChordProgression(sectionParams);
					var scaleNotes = GetScaleNotes(sectionParams.ScaleType, sectionParams.Mood);

					var sectionProviders = GenerateSectionWithOffset(scaleNotes, sectionParams,
						beatDuration, chordProgression, currentTime);
					providers.AddRange(sectionProviders);
				}

				// トランジション期間
				double transitionEnd = Math.Min(change.TimeSeconds + change.TransitionDuration,
					initialParams.DurationSeconds);

				if (transitionEnd > change.TimeSeconds)
				{
					var transitionProviders = GenerateTransition(currentParams, change.Parameters,
						change.TimeSeconds, change.TransitionDuration);
					providers.AddRange(transitionProviders);
				}

				currentTime = transitionEnd;
				currentParams = change.Parameters.Clone();
			}

			// 最後のセクション
			if (currentTime < initialParams.DurationSeconds)
			{
				var finalParams = currentParams.Clone();
				finalParams.DurationSeconds = (int)(initialParams.DurationSeconds - currentTime);

				var beatDuration = 60.0 / finalParams.Tempo;
				var chordProgression = GenerateChordProgression(finalParams);
				var scaleNotes = GetScaleNotes(finalParams.ScaleType, finalParams.Mood);

				var finalProviders = GenerateSectionWithOffset(scaleNotes, finalParams,
					beatDuration, chordProgression, currentTime);
				providers.AddRange(finalProviders);
			}

			return providers;
		}

		/// <summary>
		/// オフセット付きでセクションを生成
		/// </summary>
		private List<ISampleProvider> GenerateSectionWithOffset(List<float> scaleNotes,
			MusicParameters parameters, double beatDuration, ChordProgression progression, double offset)
		{
			var providers = new List<ISampleProvider>();

			// メロディー
			var melodyProviders = GenerateMelody(scaleNotes, parameters, beatDuration, progression);
			foreach (var provider in melodyProviders)
			{
				// オフセットを追加
				var offsetProvider = new OffsetSampleProvider(provider)
				{
					DelayBy = TimeSpan.FromSeconds(offset)
				};
				providers.Add(offsetProvider);
			}

			// 和音
			if (parameters.EnableHarmony)
			{
				var harmonyProviders = GenerateHarmony(progression, parameters, beatDuration);
				foreach (var provider in harmonyProviders)
				{
					var offsetProvider = new OffsetSampleProvider(provider)
					{
						DelayBy = TimeSpan.FromSeconds(offset)
					};
					providers.Add(offsetProvider);
				}
			}

			// ベース
			if (parameters.EnableBass)
			{
				var bassProviders = GenerateBass(progression, parameters, beatDuration);
				foreach (var provider in bassProviders)
				{
					var offsetProvider = new OffsetSampleProvider(provider)
					{
						DelayBy = TimeSpan.FromSeconds(offset)
					};
					providers.Add(offsetProvider);
				}
			}

			return providers;
		}

		/// <summary>
		/// トランジション（パラメーター間の滑らかな移行）を生成
		/// </summary>
		private List<ISampleProvider> GenerateTransition(MusicParameters fromParams,
			MusicParameters toParams, double startTime, double duration)
		{
			var providers = new List<ISampleProvider>();

			// 音量のクロスフェード
			int steps = Math.Max(1, (int)(duration * 4)); // 4分割
			double stepDuration = duration / steps;

			for (int i = 0; i < steps; i++)
			{
				double t = (double)i / steps; // 0.0 to 1.0
				double time = startTime + i * stepDuration;

				// 音量を徐々に変化
				float fromVolume = fromParams.Volume * (1.0f - (float)t);
				float toVolume = toParams.Volume * (float)t;

				// 簡単なクロスフェードトーン
				if (fromVolume > 0.01f)
				{
					var fromTone = CreateTransitionTone(440.0f, stepDuration, fromVolume);
					var fromOffset = new OffsetSampleProvider(fromTone)
					{
						DelayBy = TimeSpan.FromSeconds(time)
					};
					providers.Add(fromOffset);
				}

				if (toVolume > 0.01f)
				{
					var toTone = CreateTransitionTone(523.25f, stepDuration, toVolume);
					var toOffset = new OffsetSampleProvider(toTone)
					{
						DelayBy = TimeSpan.FromSeconds(time)
					};
					providers.Add(toOffset);
				}
			}

			return providers;
		}

		/// <summary>
		/// トランジション用のトーンを作成
		/// </summary>
		private ISampleProvider CreateTransitionTone(float frequency, double duration, float volume)
		{
			var sine = new SignalGenerator(_sampleRate, 1)
			{
				Gain = volume * 0.3f,
				Frequency = frequency,
				Type = SignalGeneratorType.Sin
			};

			return new ADSREnvelope(sine.Take(TimeSpan.FromSeconds(duration)),
				0.05, 0.1, volume * 0.5f, duration - 0.2, 0.05);
		}

		/// <summary>
		/// コード進行を生成
		/// </summary>
		public ChordProgression GenerateChordProgression(MusicParameters parameters)
		{
			var chords = new List<Chord>();
			List<List<int>> progressionPattern;

			switch (parameters.Mood)
			{
				case MusicMood.Happy:
					// I - IV - V - I など明るい進行
					progressionPattern = new List<List<int>>
					{
						new List<int> { 0, 4, 7 },    // I (Major)
                        new List<int> { 5, 9, 12 },   // IV (Major)
                        new List<int> { 7, 11, 14 },  // V (Major)
                        new List<int> { 0, 4, 7 }     // I (Major)
                    };
					break;
				case MusicMood.Sad:
					// i - iv - v - i など短調の進行
					progressionPattern = new List<List<int>>
					{
						new List<int> { 0, 3, 7 },    // i (minor)
                        new List<int> { 5, 8, 12 },   // iv (minor)
                        new List<int> { 7, 10, 14 },  // v (minor)
                        new List<int> { 0, 3, 7 }     // i (minor)
                    };
					break;
				case MusicMood.Energetic:
					// I - V - vi - IV (ポップス進行)
					progressionPattern = new List<List<int>>
					{
						new List<int> { 0, 4, 7 },
						new List<int> { 7, 11, 14 },
						new List<int> { 9, 12, 16 },
						new List<int> { 5, 9, 12 }
					};
					break;
				case MusicMood.Calm:
					progressionPattern = new List<List<int>>
					{
						new List<int> { 0, 4, 7 },
						new List<int> { 2, 5, 9 },
						new List<int> { 5, 9, 12 },
						new List<int> { 0, 4, 7 }
					};
					break;
				default:
					progressionPattern = new List<List<int>>
					{
						new List<int> { 0, 4, 7 },
						new List<int> { 5, 9, 12 },
						new List<int> { 7, 11, 14 },
						new List<int> { 0, 4, 7 }
					};
					break;
			}

			foreach (var pattern in progressionPattern)
			{
				chords.Add(new Chord { Intervals = pattern });
			}

			return new ChordProgression { Chords = chords };
		}

		/// <summary>
		/// メロディーを生成
		/// </summary>
		private List<ISampleProvider> GenerateMelody(List<float> scaleNotes, MusicParameters parameters,
			double beatDuration, ChordProgression progression)
		{
			var providers = new List<ISampleProvider>();
			double currentTime = 0;
			int chordIndex = 0;
			double chordDuration = beatDuration * 4; // 1コードあたり4拍

			// メロディーモチーフを生成（4-8音のフレーズ）
			var motifs = GenerateMelodyMotifs(scaleNotes, progression, parameters, beatDuration);
			int motifIndex = 0;
			int notesInCurrentMotif = 0;
			int currentMotifLength = motifs[0].Count;
			float lastFrequency = motifs[0][0];

			while (currentTime < parameters.DurationSeconds)
			{
				var currentChord = progression.Chords[chordIndex % progression.Chords.Count];

				// モチーフから音を取得、または新しい音を生成
				float frequency;
				if (notesInCurrentMotif < currentMotifLength)
				{
					// モチーフを使用
					frequency = motifs[motifIndex % motifs.Count][notesInCurrentMotif];
					notesInCurrentMotif++;
				}
				else
				{
					// 新しいモチーフに移行するか、変奏を加える
					if (_random.NextDouble() < 0.3) // 30%で新しいモチーフ
					{
						motifIndex++;
						notesInCurrentMotif = 0;
						currentMotifLength = motifs[motifIndex % motifs.Count].Count;
						frequency = motifs[motifIndex % motifs.Count][0];
					}
					else // 70%で前の音に近い音を選択（滑らかな進行）
					{
						frequency = SelectSmoothMelodyNote(scaleNotes, currentChord, lastFrequency);
						notesInCurrentMotif = 0;
					}
				}

				lastFrequency = frequency;
				double duration = GetNoteDuration(beatDuration, parameters.Mood);

				var tone = CreateMelodicTone(frequency, duration, parameters.Volume);
				var offset = new OffsetSampleProvider(tone)
				{
					DelayBy = TimeSpan.FromSeconds(currentTime)
				};
				providers.Add(offset);

				currentTime += duration;

				// 適度に休符を挿入
				if (ShouldAddRest(parameters.Mood))
				{
					currentTime += beatDuration * 0.25;
				}

				// コード進行を進める
				if (currentTime >= chordDuration * (chordIndex + 1))
				{
					chordIndex++;
				}
			}

			return providers;
		}

		/// <summary>
		/// メロディーモチーフを生成（繰り返し可能なフレーズ）
		/// </summary>
		private List<List<float>> GenerateMelodyMotifs(List<float> scaleNotes,
			ChordProgression progression, MusicParameters parameters, double beatDuration)
		{
			var motifs = new List<List<float>>();
			var melodyRange = scaleNotes.Skip(scaleNotes.Count / 2).ToList();

			// パターンに応じてモチーフ数を調整
			int motifCount;
			switch (parameters.PatternType)
			{
				case MelodyPattern.Repetitive:
				case MelodyPattern.Minimal:
					motifCount = _random.Next(1, 3); // 少なめ
					break;
				case MelodyPattern.Sequence:
				case MelodyPattern.Ornamental:
					motifCount = _random.Next(3, 5); // 多め
					break;
				default:
					motifCount = _random.Next(2, 4);
					break;
			}

			for (int m = 0; m < motifCount; m++)
			{
				var chord = progression.Chords[m % progression.Chords.Count];
				List<float> motif = GenerateMotifByPattern(melodyRange, chord, parameters.PatternType);
				motifs.Add(motif);
			}

			return motifs;
		}

		/// <summary>
		/// パターンに基づいてモチーフを生成
		/// </summary>
		private List<float> GenerateMotifByPattern(List<float> melodyRange, Chord chord, MelodyPattern pattern)
		{
			var motif = new List<float>();

			switch (pattern)
			{
				case MelodyPattern.Stepwise:
					motif = GenerateStepwiseMotif(melodyRange, chord);
					break;
				case MelodyPattern.Jumping:
					motif = GenerateJumpingMotif(melodyRange, chord);
					break;
				case MelodyPattern.Arpeggiated:
					motif = GenerateArpeggiatedMotif(melodyRange, chord);
					break;
				case MelodyPattern.Repetitive:
					motif = GenerateRepetitiveMotif(melodyRange, chord);
					break;
				case MelodyPattern.ScaleRun:
					motif = GenerateScaleRunMotif(melodyRange);
					break;
				case MelodyPattern.Syncopated:
					motif = GenerateSyncopatedMotif(melodyRange, chord);
					break;
				case MelodyPattern.Sequence:
					motif = GenerateSequenceMotif(melodyRange, chord);
					break;
				case MelodyPattern.Minimal:
					motif = GenerateMinimalMotif(melodyRange, chord);
					break;
				case MelodyPattern.Ornamental:
					motif = GenerateOrnamentalMotif(melodyRange, chord);
					break;
				default: // Balanced
					motif = GenerateBalancedMotif(melodyRange, chord);
					break;
			}

			return motif;
		}

		/// <summary>
		/// 順次進行中心のモチーフ（滑らか）
		/// </summary>
		private List<float> GenerateStepwiseMotif(List<float> melodyRange, Chord chord)
		{
			var motif = new List<float>();
			int length = _random.Next(5, 9);
			int startIndex = _random.Next(melodyRange.Count / 2, melodyRange.Count);

			motif.Add(melodyRange[startIndex]);

			for (int i = 1; i < length; i++)
			{
				int step = _random.Next(-2, 3); // ±2ステップ以内
				startIndex = Math.Max(0, Math.Min(melodyRange.Count - 1, startIndex + step));
				motif.Add(melodyRange[startIndex]);
			}

			return motif;
		}

		/// <summary>
		/// 跳躍進行中心のモチーフ（ダイナミック）
		/// </summary>
		private List<float> GenerateJumpingMotif(List<float> melodyRange, Chord chord)
		{
			var motif = new List<float>();
			int length = _random.Next(4, 7);

			for (int i = 0; i < length; i++)
			{
				// コードトーンを中心に大きく跳躍
				float note = SelectMelodyNoteFromChord(melodyRange, chord);
				motif.Add(note);
			}

			return motif;
		}

		/// <summary>
		/// アルペジオ風のモチーフ（和音分散）
		/// </summary>
		private List<float> GenerateArpeggiatedMotif(List<float> melodyRange, Chord chord)
		{
			var motif = new List<float>();
			var chordTones = GetChordTonesInRange(melodyRange, chord);

			// 上昇または下降のアルペジオ
			bool ascending = _random.NextDouble() < 0.5;
			if (!ascending) chordTones.Reverse();

			// 2回繰り返し
			for (int rep = 0; rep < 2; rep++)
			{
				foreach (var tone in chordTones)
				{
					motif.Add(tone);
				}
			}

			return motif;
		}

		/// <summary>
		/// 反復的なモチーフ（同じ音の繰り返し）
		/// </summary>
		private List<float> GenerateRepetitiveMotif(List<float> melodyRange, Chord chord)
		{
			var motif = new List<float>();
			float note = SelectMelodyNoteFromChord(melodyRange, chord);

			// 同じ音を3-5回繰り返し
			int repeatCount = _random.Next(3, 6);
			for (int i = 0; i < repeatCount; i++)
			{
				motif.Add(note);
			}

			// 最後に別の音へ移動
			int noteIndex = melodyRange.IndexOf(note);
			int newIndex = noteIndex + _random.Next(-3, 4);
			newIndex = Math.Max(0, Math.Min(melodyRange.Count - 1, newIndex));
			motif.Add(melodyRange[newIndex]);

			return motif;
		}

		/// <summary>
		/// スケール走句のモチーフ（上昇・下降）
		/// </summary>
		private List<float> GenerateScaleRunMotif(List<float> melodyRange)
		{
			var motif = new List<float>();
			bool ascending = _random.NextDouble() < 0.5;
			int startIndex = ascending ? 0 : melodyRange.Count - 1;
			int direction = ascending ? 1 : -1;
			int length = _random.Next(5, 8);

			for (int i = 0; i < length && startIndex >= 0 && startIndex < melodyRange.Count; i++)
			{
				motif.Add(melodyRange[startIndex]);
				startIndex += direction;
			}

			return motif;
		}

		/// <summary>
		/// シンコペーション風のモチーフ（リズミカル）
		/// </summary>
		private List<float> GenerateSyncopatedMotif(List<float> melodyRange, Chord chord)
		{
			var motif = new List<float>();
			var chordTones = GetChordTonesInRange(melodyRange, chord);

			// 短-短-長のリズムパターン
			for (int i = 0; i < 6; i++)
			{
				float note = chordTones[_random.Next(chordTones.Count)];
				motif.Add(note);
			}

			return motif;
		}

		/// <summary>
		/// シーケンスモチーフ（モチーフの移調）
		/// </summary>
		private List<float> GenerateSequenceMotif(List<float> melodyRange, Chord chord)
		{
			var motif = new List<float>();

			// 短いパターンを生成
			int patternLength = 3;
			int startIndex = _random.Next(melodyRange.Count / 2, melodyRange.Count - patternLength * 3);

			// パターンを3回、少しずつ上げて繰り返す
			for (int seq = 0; seq < 3; seq++)
			{
				for (int i = 0; i < patternLength; i++)
				{
					int index = Math.Min(melodyRange.Count - 1, startIndex + i + seq * 2);
					motif.Add(melodyRange[index]);
				}
			}

			return motif;
		}

		/// <summary>
		/// ミニマルなモチーフ（少ない音での反復）
		/// </summary>
		private List<float> GenerateMinimalMotif(List<float> melodyRange, Chord chord)
		{
			var motif = new List<float>();

			// 2-3音だけを使用
			var selectedNotes = new List<float>();
			for (int i = 0; i < _random.Next(2, 4); i++)
			{
				selectedNotes.Add(SelectMelodyNoteFromChord(melodyRange, chord));
			}

			// パターン的に繰り返す
			for (int i = 0; i < 8; i++)
			{
				motif.Add(selectedNotes[i % selectedNotes.Count]);
			}

			return motif;
		}

		/// <summary>
		/// 装飾的なモチーフ（細かい音符）
		/// </summary>
		private List<float> GenerateOrnamentalMotif(List<float> melodyRange, Chord chord)
		{
			var motif = new List<float>();
			int length = _random.Next(8, 13); // 長めのフレーズ
			int currentIndex = _random.Next(melodyRange.Count / 2, melodyRange.Count);

			for (int i = 0; i < length; i++)
			{
				motif.Add(melodyRange[currentIndex]);

				// 細かく上下に動く
				int movement = _random.Next(-2, 3);
				currentIndex = Math.Max(0, Math.Min(melodyRange.Count - 1, currentIndex + movement));
			}

			return motif;
		}

		/// <summary>
		/// バランス型のモチーフ（様々な要素の組み合わせ）
		/// </summary>
		private List<float> GenerateBalancedMotif(List<float> melodyRange, Chord chord)
		{
			var motif = new List<float>();
			int length = _random.Next(5, 8);
			float currentNote = SelectMelodyNoteFromChord(melodyRange, chord);
			motif.Add(currentNote);

			for (int i = 1; i < length; i++)
			{
				// 60%順次進行、30%コードトーン、10%跳躍
				double rand = _random.NextDouble();

				if (rand < 0.6)
				{
					// 順次進行
					int noteIndex = melodyRange.IndexOf(currentNote);
					int step = _random.Next(-2, 3);
					noteIndex = Math.Max(0, Math.Min(melodyRange.Count - 1, noteIndex + step));
					currentNote = melodyRange[noteIndex];
				}
				else if (rand < 0.9)
				{
					// コードトーン
					currentNote = SelectMelodyNoteFromChord(melodyRange, chord);
				}
				else
				{
					// 跳躍
					currentNote = melodyRange[_random.Next(melodyRange.Count / 2, melodyRange.Count)];
				}

				motif.Add(currentNote);
			}

			return motif;
		}

		/// <summary>
		/// 範囲内のコードトーンを取得
		/// </summary>
		private List<float> GetChordTonesInRange(List<float> melodyRange, Chord chord)
		{
			var chordTones = new List<float>();
			float baseFreq = melodyRange[0];

			foreach (var interval in chord.Intervals)
			{
				float freq = baseFreq * (float)Math.Pow(2, interval / 12.0);
				var closestNote = melodyRange.OrderBy(n => Math.Abs(n - freq)).FirstOrDefault();
				if (closestNote > 0 && !chordTones.Contains(closestNote))
				{
					chordTones.Add(closestNote);
				}
			}

			if (chordTones.Count == 0)
			{
				chordTones.Add(melodyRange[melodyRange.Count / 2]);
			}

			return chordTones;
		}

		/// <summary>
		/// 前の音から滑らかに進行する次の音を選択
		/// </summary>
		private float SelectSmoothMelodyNote(List<float> scaleNotes, Chord chord, float previousNote)
		{
			var melodyRange = scaleNotes.Skip(scaleNotes.Count / 2).ToList();

			// 前の音のインデックスを取得
			int previousIndex = melodyRange.IndexOf(melodyRange.OrderBy(n => Math.Abs(n - previousNote)).First());

			// 音程の跳躍を制限（±1-3ステップ）
			int maxJump = 3;
			int minJump = -3;

			// 60%の確率で順次進行（±1-2ステップ）、40%で跳躍進行
			if (_random.NextDouble() < 0.6)
			{
				maxJump = 2;
				minJump = -2;
			}

			int jump = _random.Next(minJump, maxJump + 1);
			int newIndex = previousIndex + jump;

			// 範囲内に収める
			if (newIndex < 0) newIndex = 0;
			if (newIndex >= melodyRange.Count) newIndex = melodyRange.Count - 1;

			float selectedNote = melodyRange[newIndex];

			// 30%の確率でコードトーンを優先
			if (_random.NextDouble() < 0.3)
			{
				selectedNote = SelectMelodyNoteFromChord(melodyRange, chord);
			}

			return selectedNote;
		}

		/// <summary>
		/// コードトーンからメロディー音を選択
		/// </summary>
		private float SelectMelodyNoteFromChord(List<float> melodyRange, Chord chord)
		{
			var chordTones = new List<float>();
			float baseFreq = melodyRange[0];

			foreach (var interval in chord.Intervals)
			{
				float freq = baseFreq * (float)Math.Pow(2, interval / 12.0);
				var closestNote = melodyRange.OrderBy(n => Math.Abs(n - freq)).FirstOrDefault();
				if (closestNote > 0)
				{
					chordTones.Add(closestNote);
				}
			}

			if (chordTones.Count > 0)
			{
				return chordTones[_random.Next(chordTones.Count)];
			}

			return melodyRange[_random.Next(melodyRange.Count)];
		}



		/// <summary>
		/// 和音を生成
		/// </summary>
		private List<ISampleProvider> GenerateHarmony(ChordProgression progression,
			MusicParameters parameters, double beatDuration)
		{
			var providers = new List<ISampleProvider>();
			double currentTime = 0;
			double chordDuration = beatDuration * 4;
			float baseFreq = 261.63f; // C4
			float harmonyVolume = parameters.Volume * 0.4f;

			// 曲全体の長さをカバーするまでコード進行を繰り返す
			while (currentTime < parameters.DurationSeconds)
			{
				foreach (var chord in progression.Chords)
				{
					if (currentTime >= parameters.DurationSeconds) break;

					// パターンに応じて和音を生成
					var chordProviders = GenerateChordByPattern(
						chord, baseFreq, chordDuration, currentTime,
						harmonyVolume, beatDuration, parameters.HarmonyPatternType);

					providers.AddRange(chordProviders);
					currentTime += chordDuration;
				}
			}

			return providers;
		}

		/// <summary>
		/// パターンに応じた和音を生成
		/// </summary>
		private List<ISampleProvider> GenerateChordByPattern(Chord chord, float baseFreq,
			double chordDuration, double startTime, float volume, double beatDuration, HarmonyPattern pattern)
		{
			var providers = new List<ISampleProvider>();

			switch (pattern)
			{
				case HarmonyPattern.Block:
					providers = GenerateBlockChord(chord, baseFreq, chordDuration, startTime, volume);
					break;
				case HarmonyPattern.Arpeggiated:
					providers = GenerateArpeggiatedChord(chord, baseFreq, chordDuration, startTime, volume, beatDuration);
					break;
				case HarmonyPattern.Pulsing:
					providers = GeneratePulsingChord(chord, baseFreq, chordDuration, startTime, volume, beatDuration);
					break;
				case HarmonyPattern.Sustained:
					providers = GenerateSustainedChord(chord, baseFreq, chordDuration, startTime, volume);
					break;
				case HarmonyPattern.Staccato:
					providers = GenerateStaccatoChord(chord, baseFreq, chordDuration, startTime, volume, beatDuration);
					break;
				case HarmonyPattern.RollingChord:
					providers = GenerateRollingChord(chord, baseFreq, chordDuration, startTime, volume);
					break;
				case HarmonyPattern.Alternating:
					providers = GenerateAlternatingChord(chord, baseFreq, chordDuration, startTime, volume, beatDuration);
					break;
				case HarmonyPattern.Tremolo:
					providers = GenerateTremoloChord(chord, baseFreq, chordDuration, startTime, volume, beatDuration);
					break;
				case HarmonyPattern.Pad:
					providers = GeneratePadChord(chord, baseFreq, chordDuration, startTime, volume);
					break;
				case HarmonyPattern.Rhythmic:
					providers = GenerateRhythmicChord(chord, baseFreq, chordDuration, startTime, volume, beatDuration);
					break;
			}

			return providers;
		}

		/// <summary>
		/// ブロックコード（全音同時・標準）
		/// </summary>
		private List<ISampleProvider> GenerateBlockChord(Chord chord, float baseFreq,
			double duration, double startTime, float volume)
		{
			var providers = new List<ISampleProvider>();

			foreach (var interval in chord.Intervals)
			{
				float frequency = baseFreq * (float)Math.Pow(2, interval / 12.0);
				var tone = CreateHarmonyTone(frequency, duration, volume);
				var offset = new OffsetSampleProvider(tone)
				{
					DelayBy = TimeSpan.FromSeconds(startTime)
				};
				providers.Add(offset);
			}

			return providers;
		}

		/// <summary>
		/// アルペジオコード（分散和音）
		/// </summary>
		private List<ISampleProvider> GenerateArpeggiatedChord(Chord chord, float baseFreq,
			double duration, double startTime, float volume, double beatDuration)
		{
			var providers = new List<ISampleProvider>();
			double noteLength = beatDuration * 0.75;
			double currentTime = startTime;

			// コードを繰り返しアルペジオで演奏
			while (currentTime < startTime + duration)
			{
				foreach (var interval in chord.Intervals)
				{
					if (currentTime >= startTime + duration) break;

					float frequency = baseFreq * (float)Math.Pow(2, interval / 12.0);
					var tone = CreateHarmonyTone(frequency, noteLength, volume);
					var offset = new OffsetSampleProvider(tone)
					{
						DelayBy = TimeSpan.FromSeconds(currentTime)
					};
					providers.Add(offset);
					currentTime += beatDuration * 0.5;
				}
			}

			return providers;
		}

		/// <summary>
		/// パルシングコード（リズミカルな反復）
		/// </summary>
		private List<ISampleProvider> GeneratePulsingChord(Chord chord, float baseFreq,
			double duration, double startTime, float volume, double beatDuration)
		{
			var providers = new List<ISampleProvider>();
			double pulseLength = beatDuration * 0.5;
			double currentTime = startTime;

			while (currentTime < startTime + duration)
			{
				foreach (var interval in chord.Intervals)
				{
					float frequency = baseFreq * (float)Math.Pow(2, interval / 12.0);
					var tone = CreateHarmonyTone(frequency, pulseLength, volume);
					var offset = new OffsetSampleProvider(tone)
					{
						DelayBy = TimeSpan.FromSeconds(currentTime)
					};
					providers.Add(offset);
				}
				currentTime += beatDuration;
			}

			return providers;
		}

		/// <summary>
		/// サステインコード（長く伸ばす、音量変化あり）
		/// </summary>
		private List<ISampleProvider> GenerateSustainedChord(Chord chord, float baseFreq,
			double duration, double startTime, float volume)
		{
			var providers = new List<ISampleProvider>();

			// 通常より長いリリースタイム
			foreach (var interval in chord.Intervals)
			{
				float frequency = baseFreq * (float)Math.Pow(2, interval / 12.0);
				var sine = new SignalGenerator(_sampleRate, 1)
				{
					Gain = volume,
					Frequency = frequency,
					Type = SignalGeneratorType.Sin
				};

				var envelope = new ADSREnvelope(sine.Take(TimeSpan.FromSeconds(duration)),
					0.1, 0.2, volume * 0.8f, duration - 0.5, 0.2);

				var offset = new OffsetSampleProvider(envelope)
				{
					DelayBy = TimeSpan.FromSeconds(startTime)
				};
				providers.Add(offset);
			}

			return providers;
		}

		/// <summary>
		/// スタッカートコード（短く切る）
		/// </summary>
		private List<ISampleProvider> GenerateStaccatoChord(Chord chord, float baseFreq,
			double duration, double startTime, float volume, double beatDuration)
		{
			var providers = new List<ISampleProvider>();
			double noteLength = beatDuration * 0.2; // 短く
			double currentTime = startTime;

			while (currentTime < startTime + duration)
			{
				foreach (var interval in chord.Intervals)
				{
					float frequency = baseFreq * (float)Math.Pow(2, interval / 12.0);
					var tone = CreateHarmonyTone(frequency, noteLength, volume);
					var offset = new OffsetSampleProvider(tone)
					{
						DelayBy = TimeSpan.FromSeconds(currentTime)
					};
					providers.Add(offset);
				}
				currentTime += beatDuration;
			}

			return providers;
		}

		/// <summary>
		/// ローリングコード（音を少しずつずらす）
		/// </summary>
		private List<ISampleProvider> GenerateRollingChord(Chord chord, float baseFreq,
			double duration, double startTime, float volume)
		{
			var providers = new List<ISampleProvider>();
			double delay = 0;

			foreach (var interval in chord.Intervals)
			{
				float frequency = baseFreq * (float)Math.Pow(2, interval / 12.0);
				var tone = CreateHarmonyTone(frequency, duration - delay, volume);
				var offset = new OffsetSampleProvider(tone)
				{
					DelayBy = TimeSpan.FromSeconds(startTime + delay)
				};
				providers.Add(offset);
				delay += 0.03; // 30ms ずつずらす
			}

			return providers;
		}

		/// <summary>
		/// 交互コード（高音と低音を交互に）
		/// </summary>
		private List<ISampleProvider> GenerateAlternatingChord(Chord chord, float baseFreq,
			double duration, double startTime, float volume, double beatDuration)
		{
			var providers = new List<ISampleProvider>();
			double currentTime = startTime;
			bool playLow = true;

			while (currentTime < startTime + duration)
			{
				if (playLow)
				{
					// 低音（ルート音）
					float frequency = baseFreq * (float)Math.Pow(2, chord.Intervals[0] / 12.0);
					var tone = CreateHarmonyTone(frequency, beatDuration * 0.75, volume);
					var offset = new OffsetSampleProvider(tone)
					{
						DelayBy = TimeSpan.FromSeconds(currentTime)
					};
					providers.Add(offset);
				}
				else
				{
					// 高音（残りの音）
					for (int i = 1; i < chord.Intervals.Count; i++)
					{
						float frequency = baseFreq * (float)Math.Pow(2, chord.Intervals[i] / 12.0);
						var tone = CreateHarmonyTone(frequency, beatDuration * 0.75, volume * 0.8f);
						var offset = new OffsetSampleProvider(tone)
						{
							DelayBy = TimeSpan.FromSeconds(currentTime)
						};
						providers.Add(offset);
					}
				}

				playLow = !playLow;
				currentTime += beatDuration;
			}

			return providers;
		}

		/// <summary>
		/// トレモロコード（素早い反復）
		/// </summary>
		private List<ISampleProvider> GenerateTremoloChord(Chord chord, float baseFreq,
			double duration, double startTime, float volume, double beatDuration)
		{
			var providers = new List<ISampleProvider>();
			double noteLength = beatDuration * 0.15;
			double currentTime = startTime;

			while (currentTime < startTime + duration)
			{
				foreach (var interval in chord.Intervals)
				{
					float frequency = baseFreq * (float)Math.Pow(2, interval / 12.0);
					var tone = CreateHarmonyTone(frequency, noteLength, volume);
					var offset = new OffsetSampleProvider(tone)
					{
						DelayBy = TimeSpan.FromSeconds(currentTime)
					};
					providers.Add(offset);
				}
				currentTime += beatDuration * 0.25; // 速い反復
			}

			return providers;
		}

		/// <summary>
		/// パッドコード（柔らかく持続）
		/// </summary>
		private List<ISampleProvider> GeneratePadChord(Chord chord, float baseFreq,
			double duration, double startTime, float volume)
		{
			var providers = new List<ISampleProvider>();

			foreach (var interval in chord.Intervals)
			{
				float frequency = baseFreq * (float)Math.Pow(2, interval / 12.0);
				var sine = new SignalGenerator(_sampleRate, 1)
				{
					Gain = volume * 0.6f, // より柔らかく
					Frequency = frequency,
					Type = SignalGeneratorType.Sin
				};

				// 非常に柔らかいアタックとリリース
				var envelope = new ADSREnvelope(sine.Take(TimeSpan.FromSeconds(duration)),
					0.3, 0.3, volume * 0.7f, duration - 0.8, 0.2);

				var offset = new OffsetSampleProvider(envelope)
				{
					DelayBy = TimeSpan.FromSeconds(startTime)
				};
				providers.Add(offset);
			}

			return providers;
		}

		/// <summary>
		/// リズミックコード（複雑なリズムパターン）
		/// </summary>
		private List<ISampleProvider> GenerateRhythmicChord(Chord chord, float baseFreq,
			double duration, double startTime, float volume, double beatDuration)
		{
			var providers = new List<ISampleProvider>();

			// リズムパターン: 長-短-短-長
			double[] pattern = { 1.0, 0.5, 0.5, 1.0, 0.5, 0.5 };
			double currentTime = startTime;
			int patternIndex = 0;

			while (currentTime < startTime + duration)
			{
				double noteLength = beatDuration * pattern[patternIndex % pattern.Length];

				foreach (var interval in chord.Intervals)
				{
					if (currentTime >= startTime + duration) break;

					float frequency = baseFreq * (float)Math.Pow(2, interval / 12.0);
					var tone = CreateHarmonyTone(frequency, noteLength * 0.9, volume);
					var offset = new OffsetSampleProvider(tone)
					{
						DelayBy = TimeSpan.FromSeconds(currentTime)
					};
					providers.Add(offset);
				}

				currentTime += noteLength;
				patternIndex++;
			}

			return providers;
		}

		/// <summary>
		/// ベースラインを生成
		/// </summary>
		private List<ISampleProvider> GenerateBass(ChordProgression progression,
			MusicParameters parameters, double beatDuration)
		{
			var providers = new List<ISampleProvider>();
			double currentTime = 0;
			float baseFreq = 130.81f; // C3 (低音)
			float bassVolume = parameters.Volume * 0.5f;

			// 曲全体の長さをカバーするまでコード進行を繰り返す
			while (currentTime < parameters.DurationSeconds)
			{
				foreach (var chord in progression.Chords)
				{
					if (currentTime >= parameters.DurationSeconds) break;

					// ルート音を演奏
					float rootFreq = baseFreq * (float)Math.Pow(2, chord.Intervals[0] / 12.0);

					// 4拍の間にルート音を2回鳴らす
					for (int i = 0; i < 2; i++)
					{
						if (currentTime >= parameters.DurationSeconds) break;

						var tone = CreateBassTone(rootFreq, beatDuration * 1.5, bassVolume);
						var offset = new OffsetSampleProvider(tone)
						{
							DelayBy = TimeSpan.FromSeconds(currentTime)
						};
						providers.Add(offset);
						currentTime += beatDuration * 2;
					}
				}
			}

			return providers;
		}

		/// <summary>
		/// スケールの音程を取得
		/// </summary>
		public List<float> GetScaleNotes(Scale scaleType, MusicMood mood)
		{
			float baseFreq = 261.63f; // C4
			int octaveRange = mood == MusicMood.Energetic ? 3 : 2;
			List<int> intervals;

			switch (scaleType)
			{
				case Scale.Major:
					intervals = new List<int> { 0, 2, 4, 5, 7, 9, 11 };
					break;
				case Scale.Minor:
					intervals = new List<int> { 0, 2, 3, 5, 7, 8, 10 };
					break;
				case Scale.Pentatonic:
					intervals = new List<int> { 0, 2, 4, 7, 9 };
					break;
				case Scale.Blues:
					intervals = new List<int> { 0, 3, 5, 6, 7, 10 };
					break;
				default:
					intervals = new List<int> { 0, 2, 4, 5, 7, 9, 11 };
					break;
			}

			var notes = new List<float>();
			for (int octave = 0; octave < octaveRange; octave++)
			{
				foreach (var interval in intervals)
				{
					float freq = baseFreq * (float)Math.Pow(2, (octave * 12 + interval) / 12.0);
					notes.Add(freq);
				}
			}

			return notes;
		}

		/// <summary>
		/// 音符の長さを決定
		/// </summary>
		private double GetNoteDuration(double beatDuration, MusicMood mood)
		{
			double[] durations;

			switch (mood)
			{
				case MusicMood.Energetic:
					durations = new[] { 0.25, 0.5, 0.5, 1.0 };
					break;
				case MusicMood.Calm:
					durations = new[] { 1.0, 1.5, 2.0, 2.0 };
					break;
				case MusicMood.Happy:
					durations = new[] { 0.5, 0.5, 1.0, 1.0 };
					break;
				case MusicMood.Sad:
					durations = new[] { 1.0, 1.5, 2.0 };
					break;
				case MusicMood.Mysterious:
					durations = new[] { 0.5, 1.0, 1.5, 2.0 };
					break;
				default:
					durations = new[] { 0.5, 1.0, 1.0, 2.0 };
					break;
			}

			return beatDuration * durations[_random.Next(durations.Length)];
		}

		/// <summary>
		/// 休符を追加するか判定
		/// </summary>
		private bool ShouldAddRest(MusicMood mood)
		{
			double probability;

			switch (mood)
			{
				case MusicMood.Calm:
					probability = 0.3;
					break;
				case MusicMood.Mysterious:
					probability = 0.4;
					break;
				case MusicMood.Energetic:
					probability = 0.1;
					break;
				default:
					probability = 0.2;
					break;
			}

			return _random.NextDouble() < probability;
		}

		/// <summary>
		/// メロディー用トーンを生成
		/// </summary>
		private ISampleProvider CreateMelodicTone(float frequency, double duration, float volume)
		{
			var sine = new SignalGenerator(_sampleRate, 1)
			{
				Gain = volume * 0.7f, // メロディーは控えめに
				Frequency = frequency,
				Type = SignalGeneratorType.Sin
			};

			return new ADSREnvelope(sine.Take(TimeSpan.FromSeconds(duration)),
				0.01, 0.05, volume * 0.6f, duration - 0.08, 0.02);
		}

		/// <summary>
		/// 和音用トーンを生成
		/// </summary>
		private ISampleProvider CreateHarmonyTone(float frequency, double duration, float volume)
		{
			var sine = new SignalGenerator(_sampleRate, 1)
			{
				Gain = volume,
				Frequency = frequency,
				Type = SignalGeneratorType.Sin
			};

			// 和音は柔らかく長めのアタック
			return new ADSREnvelope(sine.Take(TimeSpan.FromSeconds(duration)),
				0.05, 0.1, volume * 0.7f, duration - 0.2, 0.05);
		}

		/// <summary>
		/// ベース用トーンを生成
		/// </summary>
		private ISampleProvider CreateBassTone(float frequency, double duration, float volume)
		{
			var sine = new SignalGenerator(_sampleRate, 1)
			{
				Gain = volume,
				Frequency = frequency,
				Type = SignalGeneratorType.Sin
			};

			// ベースははっきりとしたアタック
			return new ADSREnvelope(sine.Take(TimeSpan.FromSeconds(duration)),
				0.005, 0.03, volume * 0.8f, duration - 0.1, 0.067);
		}
	}

	/// <summary>
	/// ADSRエンベロープジェネレーター（破裂音防止強化版）
	/// </summary>
	public class ADSREnvelope : ISampleProvider
	{
		private readonly ISampleProvider _source;
		private readonly double _attackTime, _decayTime, _releaseTime, _sustainLevel;
		private readonly double _totalDuration;
		private int _sampleIndex;
		private readonly int _attackSamples, _decaySamples, _releaseSamples, _totalSamples;

		public WaveFormat WaveFormat { get { return _source.WaveFormat; } }

		public ADSREnvelope(ISampleProvider source, double attack, double decay,
			float sustain, double hold, double release)
		{
			_source = source;
			_attackTime = attack;
			_decayTime = decay;
			_sustainLevel = sustain;
			_releaseTime = release;
			_totalDuration = attack + decay + hold + release;

			_attackSamples = (int)(attack * WaveFormat.SampleRate);
			_decaySamples = (int)(decay * WaveFormat.SampleRate);
			_releaseSamples = (int)(release * WaveFormat.SampleRate);
			_totalSamples = (int)(_totalDuration * WaveFormat.SampleRate);
		}

		public int Read(float[] buffer, int offset, int count)
		{
			int read = _source.Read(buffer, offset, count);

			for (int i = 0; i < read; i++)
			{
				float envelope = CalculateEnvelope(_sampleIndex);
				buffer[offset + i] *= envelope;
				_sampleIndex++;
			}

			return read;
		}

		private float CalculateEnvelope(int sampleIndex)
		{
			if (sampleIndex < _attackSamples)
			{
				// アタック: 滑らかな立ち上がり（サイン曲線）
				float t = (float)sampleIndex / _attackSamples;
				return (float)Math.Sin(t * Math.PI / 2);
			}
			else if (sampleIndex < _attackSamples + _decaySamples)
			{
				// ディケイ
				float t = (float)(sampleIndex - _attackSamples) / _decaySamples;
				return 1.0f - (1.0f - (float)_sustainLevel) * t;
			}
			else if (sampleIndex > _totalSamples - _releaseSamples)
			{
				// リリース: 滑らかな減衰（サイン曲線）
				float t = (float)(_totalSamples - sampleIndex) / _releaseSamples;
				return (float)_sustainLevel * (float)Math.Sin(t * Math.PI / 2);
			}
			else
			{
				// サステイン
				return (float)_sustainLevel;
			}
		}
	}

	/// <summary>
	/// ソフトリミッター（クリッピング防止）
	/// </summary>
	public class SoftLimiter : ISampleProvider
	{
		private readonly ISampleProvider _source;
		private readonly float _threshold;

		public WaveFormat WaveFormat { get { return _source.WaveFormat; } }

		public SoftLimiter(ISampleProvider source, float threshold)
		{
			_source = source;
			_threshold = threshold;
		}

		public int Read(float[] buffer, int offset, int count)
		{
			int read = _source.Read(buffer, offset, count);

			for (int i = 0; i < read; i++)
			{
				float sample = buffer[offset + i];

				// ソフトクリッピング（tanh関数を使用）
				if (Math.Abs(sample) > _threshold)
				{
					sample = (float)Math.Tanh(sample / _threshold) * _threshold;
				}

				buffer[offset + i] = sample;
			}

			return read;
		}
	}

	/// <summary>
	/// パディング付きプロバイダー（最後まで無音で埋める）
	/// </summary>
	public class PaddedProvider : ISampleProvider
	{
		private readonly ISampleProvider _source;
		private readonly long _totalSamples;
		private long _samplesRead;

		public WaveFormat WaveFormat { get { return _source.WaveFormat; } }

		public PaddedProvider(ISampleProvider source, TimeSpan totalDuration)
		{
			_source = source;
			_totalSamples = (long)(totalDuration.TotalSeconds * WaveFormat.SampleRate * WaveFormat.Channels);
			_samplesRead = 0;
		}

		public int Read(float[] buffer, int offset, int count)
		{
			int samplesNeeded = (int)Math.Min(count, _totalSamples - _samplesRead);

			if (samplesNeeded <= 0)
			{
				return 0;
			}

			int read = _source.Read(buffer, offset, samplesNeeded);

			// ソースが終わったら無音で埋める
			if (read < samplesNeeded)
			{
				Array.Clear(buffer, offset + read, samplesNeeded - read);
				read = samplesNeeded;
			}

			_samplesRead += read;
			return read;
		}
	}

	/// <summary>
	/// リアルタイムでパラメーター変更可能な動的音楽プロバイダー
	/// </summary>
	public class DynamicMusicProvider : ISampleProvider, IDisposable
	{
		private readonly RandomMusicGenerator _generator;
		private readonly int _sampleRate;
		private MusicParameters _currentParams;
		private MusicParameters _targetParams;
		private double _transitionProgress;
		private double _transitionDuration;
		private bool _isTransitioning;

		private readonly object _lockObject = new object();
		private long _totalSamplesRead;
		private readonly int _bufferSize = 4096;

		// 音楽生成用のキャッシュ
		private Queue<float> _audioBuffer;
		private System.Threading.Thread _generationThread;
		private bool _isRunning;

		public WaveFormat WaveFormat { get; private set; }

		public double CurrentTimeSeconds
		{
			get { return (double)_totalSamplesRead / _sampleRate; }
		}

		public DynamicMusicProvider(RandomMusicGenerator generator, int sampleRate, MusicParameters initialParams)
		{
			_generator = generator;
			_sampleRate = sampleRate;
			_currentParams = initialParams.Clone();
			_targetParams = null;
			_isTransitioning = false;
			_transitionProgress = 0;
			_audioBuffer = new Queue<float>();

			WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);

			// バックグラウンドで音楽生成
			_isRunning = true;
			_generationThread = new System.Threading.Thread(GenerateAudioLoop);
			_generationThread.IsBackground = true;
			_generationThread.Start();
		}

		public void UpdateParameters(MusicParameters newParams, double transitionSeconds)
		{
			lock (_lockObject)
			{
				_targetParams = newParams.Clone();
				_transitionDuration = transitionSeconds;
				_transitionProgress = 0;
				_isTransitioning = true;
			}
		}

		private void GenerateAudioLoop()
		{
			double currentTime = 0;
			double noteTime = 0;
			double beatDuration = 60.0 / _currentParams.Tempo;

			var scaleNotes = _generator.GetScaleNotes(_currentParams.ScaleType, _currentParams.Mood);
			var melodyRange = scaleNotes.Skip(scaleNotes.Count / 2).ToList();
			var chordProgression = _generator.GenerateChordProgression(_currentParams);

			int chordIndex = 0;
			int noteIndex = 0;
			float lastFrequency = melodyRange[melodyRange.Count / 2];

			while (_isRunning)
			{
				lock (_lockObject)
				{
					// トランジション処理
					if (_isTransitioning && _targetParams != null)
					{
						_transitionProgress += 0.05; // 50ms単位で進行

						if (_transitionProgress >= _transitionDuration)
						{
							_currentParams = _targetParams.Clone();
							_targetParams = null;
							_isTransitioning = false;
							_transitionProgress = 0;

							// パラメーター更新時に再生成
							beatDuration = 60.0 / _currentParams.Tempo;
							scaleNotes = _generator.GetScaleNotes(_currentParams.ScaleType, _currentParams.Mood);
							melodyRange = scaleNotes.Skip(scaleNotes.Count / 2).ToList();
							chordProgression = _generator.GenerateChordProgression(_currentParams);
						}
					}

					// バッファに余裕がある場合のみ生成
					if (_audioBuffer.Count < _bufferSize * 10)
					{
						// 現在のパラメーターで音を生成
						var effectiveParams = GetEffectiveParameters();
						float volume = effectiveParams.Volume;

						// メロディー音を生成
						if (currentTime >= noteTime)
						{
							var currentChord = chordProgression.Chords[chordIndex % chordProgression.Chords.Count];
							float frequency = SelectNextNote(melodyRange, currentChord, lastFrequency, effectiveParams);
							lastFrequency = frequency;

							double duration = GetNoteDurationValue(beatDuration, effectiveParams.Mood);
							int samples = (int)(duration * _sampleRate);

							// 簡易的な音生成
							for (int i = 0; i < samples; i++)
							{
								double t = (double)i / _sampleRate;
								float envelope = CalculateSimpleEnvelope(t, duration);
								float sample = (float)(Math.Sin(2 * Math.PI * frequency * t) * volume * 0.3f * envelope);
								_audioBuffer.Enqueue(sample);
							}

							noteTime = currentTime + duration;
							noteIndex++;

							// コード進行
							if (noteIndex % 8 == 0)
							{
								chordIndex++;
							}
						}

						currentTime += 0.01; // 10ms進める
					}
					else
					{
						System.Threading.Thread.Sleep(10);
					}
				}
			}
		}

		private MusicParameters GetEffectiveParameters()
		{
			if (!_isTransitioning || _targetParams == null)
			{
				return _currentParams;
			}

			// トランジション中は中間的なパラメーターを返す
			float t = (float)(_transitionProgress / _transitionDuration);
			t = Math.Max(0, Math.Min(1, t));

			var blended = new MusicParameters
			{
				Tempo = (int)(_currentParams.Tempo * (1 - t) + _targetParams.Tempo * t),
				Volume = _currentParams.Volume * (1 - t) + _targetParams.Volume * t,
				Mood = t < 0.5f ? _currentParams.Mood : _targetParams.Mood,
				ScaleType = t < 0.5f ? _currentParams.ScaleType : _targetParams.ScaleType,
				PatternType = t < 0.5f ? _currentParams.PatternType : _targetParams.PatternType,
				HarmonyPatternType = t < 0.5f ? _currentParams.HarmonyPatternType : _targetParams.HarmonyPatternType,
				EnableHarmony = _targetParams.EnableHarmony,
				EnableBass = _targetParams.EnableBass
			};

			return blended;
		}

		private float SelectNextNote(List<float> melodyRange, Chord chord, float lastNote, MusicParameters param)
		{
			int lastIndex = melodyRange.IndexOf(melodyRange.OrderBy(n => Math.Abs(n - lastNote)).First());
			int jump = _generator.Random.Next(-3, 4);
			int newIndex = Math.Max(0, Math.Min(melodyRange.Count - 1, lastIndex + jump));
			return melodyRange[newIndex];
		}

		private double GetNoteDurationValue(double beatDuration, MusicMood mood)
		{
			double[] durations;
			switch (mood)
			{
				case MusicMood.Energetic:
					durations = new[] { 0.25, 0.5 };
					break;
				case MusicMood.Calm:
					durations = new[] { 1.0, 1.5 };
					break;
				default:
					durations = new[] { 0.5, 1.0 };
					break;
			}
			return beatDuration * durations[_generator.Random.Next(durations.Length)];
		}

		private float CalculateSimpleEnvelope(double t, double duration)
		{
			double attack = 0.01;
			double release = 0.05;

			if (t < attack)
			{
				return (float)(t / attack);
			}
			else if (t > duration - release)
			{
				return (float)((duration - t) / release);
			}
			return 1.0f;
		}

		public int Read(float[] buffer, int offset, int count)
		{
			lock (_lockObject)
			{
				int samplesRead = 0;

				while (samplesRead < count && _audioBuffer.Count > 0)
				{
					buffer[offset + samplesRead] = _audioBuffer.Dequeue();
					samplesRead++;
				}

				// バッファが空の場合は無音で埋める
				while (samplesRead < count)
				{
					buffer[offset + samplesRead] = 0;
					samplesRead++;
				}

				_totalSamplesRead += samplesRead;
				return samplesRead;
			}
		}

		public void Dispose()
		{
			_isRunning = false;
			if (_generationThread != null && _generationThread.IsAlive)
			{
				_generationThread.Join(1000);
			}
		}
	}
}