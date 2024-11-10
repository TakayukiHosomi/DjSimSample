using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using NAudio.Wave.SampleProviders;

namespace DjSimSample
{
    public partial class MainWindow : Window
    {
        #region 変数群

        // 各オーディオ取り込み再生用変数
        private IWavePlayer leftWavePlayer;
        private AudioFileReader leftAudioFile;
        private IWavePlayer rightWavePlayer;
        private AudioFileReader rightAudioFile;

        private string leftFilePath;
        private string rightFilePath;

        //効果音再生用変数
        private IWavePlayer outputSeDevice;
        private AudioFileReader seAudioFileReader;

        //アニメーションするための変数
        private DoubleAnimation leftAnimation;
        private DoubleAnimation rightAnimation;
        private RotateTransform leftRotateTransform;
        private RotateTransform rightRotateTransform;

        // 現在の回転角度を保持する変数
        private double leftCurrentAngle = 0;
        private double rightCurrentAngle = 0;

        //オーディオの状態フラグ このフラグで状態管理する
        //再生中か？
        private bool isLeftPlaying = false;
        private bool isRightPlaying = false;

        // 一時停止中か？
        private bool isLeftPaused = false;
        private bool isRightPaused = false;

        //キー長押しフラグ
        private bool isAKeydown = false;
        private bool is4Keydown = false;

        //オーディオの再生時間管理用
        private TimeSpan left_ts;
        private TimeSpan right_ts;
        private Stopwatch leftStopwatch = new Stopwatch();
        private Stopwatch rightStopwatch = new Stopwatch();

        //効果音ボタンパス
        private string soundsFolder = "Sounds";         //効果音格納フォルダ名
        private string btn1SoundFile = "horn.mp3";   //btn1効果音
        private string btn2SoundFile = "beyond.mp3";    //btn2効果音

        //効果音スクラッチパス
        private string scratch_back = "scratch_back.mp3";   //スクラッチバック効果音
        private string scratch_play = "scratch_play.mp3";   ////スクラッチ終わり効果音

        #endregion

        #region コンストラクタ

        public MainWindow()
        {
            InitializeComponent();
            this.KeyUp += new KeyEventHandler(Windows_KeyUp);//キーバインドを確実に効かせたい
        }

        #endregion

        #region オーディオ関連

        // オーディオ取り込み 左右
        private void ImportLeftAudio_Click(object sender, RoutedEventArgs e)
        {
            if (!isLeftPlaying)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav";

                if (openFileDialog.ShowDialog() == true)
                {
                    leftFilePath = openFileDialog.FileName;
                    LeftRecord.Source = new BitmapImage(new Uri("pack://application:,,,/Images/LeftRecord_on.png"));
                    left_ts = TimeSpan.Zero;    //オーディオを切り替えたらタイムスパンリセット

                    // タイトルを表示
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(leftFilePath);
                    LeftAudioTitle.Text = fileName;
                }
            }
        }

        private void ImportRightAudio_Click(object sender, RoutedEventArgs e)
        {

            if (!isRightPlaying)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav";

                if (openFileDialog.ShowDialog() == true)
                {
                    rightFilePath = openFileDialog.FileName;
                    RightRecord.Source = new BitmapImage(new Uri("pack://application:,,,/Images/RightRecord_on.png"));
                    right_ts = TimeSpan.Zero;   //オーディオを切り替えたらタイムスパンリセット

                    // タイトルを表示
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(rightFilePath);
                    RightAudioTitle.Text = fileName;
                }
            }
        }

        // オーディオ再生停止 左
        private void PlayLeftAudio(string filePath)
        {
            if (!isLeftPlaying)
            {
                isLeftPlaying = true;
                leftWavePlayer?.Stop();
                leftWavePlayer?.Dispose();
                leftAudioFile?.Dispose();

                leftWavePlayer = new WaveOutEvent();
                leftAudioFile = new AudioFileReader(filePath);
                leftWavePlayer.Init(leftAudioFile);
                leftWavePlayer.Play();

                // 左のレコードを回転
                RotateRecord(LeftRecord, true); //isLeft = trueで左判定
            }
        }

        private void StopLeftAudio()
        {
            leftWavePlayer?.Stop();
            leftAudioFile?.Dispose();
            leftWavePlayer?.Dispose();

            // 左のレコードの回転を停止
            StopRotateRecord(LeftRecord, true);　//isLeft = trueで左判定
            leftCurrentAngle = 0;
            isLeftPlaying = false;
        }

        // オーディオ再生停止 右
        private void PlayRightAudio(string filePath)
        {
            if (!isRightPlaying)
            {
                isRightPlaying = true;
                rightWavePlayer?.Stop();
                rightWavePlayer?.Dispose();
                rightAudioFile?.Dispose();

                rightWavePlayer = new WaveOutEvent();
                rightAudioFile = new AudioFileReader(filePath);
                rightWavePlayer.Init(rightAudioFile);
                rightWavePlayer.Play();

                // 右のレコードを回転
                RotateRecord(RightRecord, false);　//isLeft = falseで右判定
            }
        }

        private void StopRightAudio()
        {
            rightWavePlayer?.Stop();
            rightAudioFile?.Dispose();
            rightWavePlayer?.Dispose();

            // 右のレコードの回転を停止
            StopRotateRecord(RightRecord, false);　//isLeft = falseで右判定
            rightCurrentAngle = 0;
            isRightPlaying = false;
        }

        // オーディオ一時停止 左
        private void TogglePauseLeftAudio()
        {
            if (leftWavePlayer != null && leftWavePlayer.PlaybackState == PlaybackState.Playing)
            {
                leftWavePlayer.Pause();
                PauseLeftRecord(); // レコードの回転を一時停止
                isLeftPaused = true;
            }
            else if (leftWavePlayer != null && leftWavePlayer.PlaybackState == PlaybackState.Paused)
            {
                leftWavePlayer.Play();
                ResumeLeftRecord(); // レコードの順回転を再開
                isLeftPaused = false;
            }
        }

        // オーディオ一時停止 右
        private void TogglePauseRightAudio()
        {
            if (rightWavePlayer != null && rightWavePlayer.PlaybackState == PlaybackState.Playing)
            {
                rightWavePlayer.Pause();
                PauseRightRecord(); // レコードの回転を一時停止
                isRightPaused = true;
            }
            else if (rightWavePlayer != null && rightWavePlayer.PlaybackState == PlaybackState.Paused)
            {
                rightWavePlayer.Play();
                ResumeRightRecord(); // レコードの順回転を再開
                isRightPaused = false;
            }
        }

        //スクラッチ　バック、プレイ
        private void ScratchBackSound()
        {
            // プロジェクトのルートディレクトリのパスを取得
            string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\"));

            // 効果音ファイルのパスを指定
            string soundFilePath = System.IO.Path.Combine(projectRoot, soundsFolder, scratch_back);

            // AudioFileReader と WaveOutEvent オブジェクトを生成。
            var audioFile = new AudioFileReader(soundFilePath);
            var outputDevice = new WaveOutEvent();

            // AudioFileReader を WaveOutEvent に設定
            outputDevice.Init(audioFile);
            outputDevice.Play();
        }

        private void ScratchPlaySound()
        {
            // プロジェクトのルートディレクトリのパスを取得
            string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\"));

            // 効果音ファイルのパスを指定
            string soundFilePath = System.IO.Path.Combine(projectRoot, soundsFolder, scratch_play);

            // AudioFileReader と WaveOutEvent オブジェクトを生成。
            var audioFile = new AudioFileReader(soundFilePath);
            var outputDevice = new WaveOutEvent();

            // AudioFileReader を WaveOutEvent に設定
            outputDevice.Init(audioFile);
            outputDevice.Play();
        }

        #endregion

        #region レコード回転アニメーション
        private void RotateRecord(System.Windows.Controls.Image record, bool isLeft)    //左右のレコードを判別して回転させる
        {
            var rotateTransform = new RotateTransform();
            record.RenderTransform = rotateTransform;
            record.RenderTransformOrigin = new Point(0.5, 0.5);

            double startAngle = isLeft ? leftCurrentAngle : rightCurrentAngle;  //左じゃなかったら右を回転

            var animation = new DoubleAnimation
            {
                From = startAngle,
                To = startAngle + 360,　//右回転
                Duration = new Duration(TimeSpan.FromSeconds(2)),　//二秒で一回転
                RepeatBehavior = RepeatBehavior.Forever
            };
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);

            //アニメーション状態の保存
            if (isLeft)
            {
                leftAnimation = animation;
                leftRotateTransform = rotateTransform;
            }
            else
            {
                rightAnimation = animation;
                rightRotateTransform = rotateTransform;
            }
        }

        //回転停止
        private void StopRotateRecord(System.Windows.Controls.Image record, bool isLeft)
        {
            var rotateTransform = record.RenderTransform as RotateTransform;
            if (rotateTransform != null)
            {
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
            }

            //角度リセット
            if (isLeft)
            {
                leftCurrentAngle = 0;
            }
            else
            {
                rightCurrentAngle = 0;
            }
        }

        // レコードの回転を一時停止　左右
        private void PauseLeftRecord()
        {
            if (leftRotateTransform != null)
            {
                leftCurrentAngle = leftRotateTransform.Angle;
                leftRotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
                leftRotateTransform.Angle = leftCurrentAngle; // 角度を保持
            }
        }

        private void PauseRightRecord()
        {
            if (rightRotateTransform != null)
            {
                rightCurrentAngle = rightRotateTransform.Angle;
                rightRotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
                rightRotateTransform.Angle = rightCurrentAngle; // 角度を保持
            }
        }

        // 通常の回転に戻すメソッド　左右
        private void ResumeLeftRecord()
        {
            if (leftRotateTransform != null)
            {
                var animation = new DoubleAnimation
                {
                    From = leftCurrentAngle,
                    To = leftCurrentAngle + 360,
                    Duration = new Duration(TimeSpan.FromSeconds(2)),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                leftRotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
            }
        }

        private void ResumeRightRecord()
        {
            if (rightRotateTransform != null && rightAnimation != null)
            {
                var animation = new DoubleAnimation
                {
                    From = rightCurrentAngle,
                    To = rightCurrentAngle + 360,
                    Duration = new Duration(TimeSpan.FromSeconds(2)),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                rightRotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
            }
        }

        // 逆回転
        private void ReverseLeftRecord()
        {
            if (leftRotateTransform != null && !isLeftPaused)
            {
                leftCurrentAngle = leftRotateTransform.Angle;
                leftRotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
                leftRotateTransform.Angle = leftCurrentAngle; // 角度を保持

                var animation = new DoubleAnimation
                {
                    From = leftCurrentAngle,
                    To = leftCurrentAngle - 360, //逆回転
                    Duration = new Duration(TimeSpan.FromSeconds(1)),　//逆回転時は倍速
                    RepeatBehavior = RepeatBehavior.Forever
                };
                leftRotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
            }
        }

        private void ReverseRightRecord()
        {
            if (rightRotateTransform != null && !isRightPaused)
            {
                rightCurrentAngle = rightRotateTransform.Angle;
                rightRotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
                rightRotateTransform.Angle = rightCurrentAngle; // 角度を保持

                var animation = new DoubleAnimation
                {
                    From = rightCurrentAngle,
                    To = rightCurrentAngle - 360,　//逆回転
                    Duration = new Duration(TimeSpan.FromSeconds(1)),　//逆回転時は倍速
                    RepeatBehavior = RepeatBehavior.Forever
                };
                rightRotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
            }
        }

        #endregion

        #region　ボタンイベントハンドラー

        // ボタンイベントハンドラー
        private void PlayLeftAudio_Click(object sender, RoutedEventArgs e)
        {
            if (leftFilePath != null)
                PlayLeftAudio(leftFilePath);
        }

        private void StopLeftAudio_Click(object sender, RoutedEventArgs e)
        {
            StopLeftAudio();
        }

        private void PlayRightAudio_Click(object sender, RoutedEventArgs e)
        {
            if (rightFilePath != null)
                PlayRightAudio(rightFilePath);
        }

        private void StopRightAudio_Click(object sender, RoutedEventArgs e)
        {
            StopRightAudio();
        }

        private void Btn1_Click(object sender, RoutedEventArgs e)
        {
            if (outputSeDevice != null)
            { 
                outputSeDevice.Stop(); 
                outputSeDevice.Dispose(); 
                seAudioFileReader.Dispose();
            }

            // プロジェクトのルートディレクトリのパスを取得
            string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\"));

            // 効果音ファイルのパスを指定
            string soundFilePath = System.IO.Path.Combine(projectRoot, soundsFolder, btn1SoundFile);

            // AudioFileReader と WaveOutEvent オブジェクトを生成。
            var audioFile = new AudioFileReader(soundFilePath);

            // 音量を増幅するためにサンプルプロバイダーをラップする
            var sampleChannel = new SampleChannel(audioFile, true);
            sampleChannel.Volume = 3.0f; // 音量を2倍に増幅

            var outputDevice = new WaveOutEvent();
            
            // AudioFileReader を WaveOutEvent に設定
            outputDevice.Init(audioFile);
            // スライダーの値を音量に反映
            outputDevice.Volume = (float)VolumeSlider.Value;
            outputDevice.Play();
        }

        private void Btn2_Click(object sender, RoutedEventArgs e)
        {
            if (outputSeDevice != null)
            {
                outputSeDevice.Stop();
                outputSeDevice.Dispose();
                seAudioFileReader.Dispose();
            }

            // プロジェクトのルートディレクトリのパスを取得
            string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\"));

            // 効果音ファイルのパスを指定
            string soundFilePath = System.IO.Path.Combine(projectRoot, soundsFolder, btn2SoundFile);

            // AudioFileReader と WaveOutEvent オブジェクトを生成。
            var audioFile = new AudioFileReader(soundFilePath);

            // 音量を増幅するためにサンプルプロバイダーをラップする
            var sampleChannel = new SampleChannel(audioFile, true);
            sampleChannel.Volume = 3.0f; // 音量を2倍に増幅

            var outputDevice = new WaveOutEvent();

            // AudioFileReader を WaveOutEvent に設定
            outputDevice.Init(audioFile);
            // スライダーの値を音量に反映
            outputDevice.Volume = (float)VolumeSlider.Value;
            outputDevice.Play();
        }

        #endregion

        #region キーボードハンドラー

                // キーボードショートカット
                private void Window_KeyDown(object sender, KeyEventArgs e)
                {
                    switch (e.Key)
                    {
                        case Key.D: // Dキーで左のオーディオを再生
                            if (leftFilePath != null && !isLeftPaused)
                                PlayLeftAudio(leftFilePath);
                            else if (isLeftPaused)
                                TogglePauseLeftAudio();
                            break;

                        case Key.S: // Sキーで左のオーディオを一時停止
                            TogglePauseLeftAudio();
                            break;

                        case Key.A: // Aキーで左のオーディオをSB　
                    
                            //RewindLeftAudio(TimeSpan.FromMilliseconds(reverseMilliSec)); //（設定変数）ミリ秒巻き戻し//同期が取れない。不採用
                    
                            if (!isAKeydown && !isLeftPaused)    //Aキーが最初に押された時かつ一時停止中でない
                            {
                                leftAudioFile.Volume = 0.0f;　   //ボリュームをゼロにしてミュート
                                left_ts = leftAudioFile.CurrentTime;　   //再生時間取得
                                leftStopwatch.Restart();　//巻き戻し時間計測用ストップウォッチスタート
                                ScratchBackSound(); //ScratchBackSound();//スクラッチ音を挿入予定
                                isAKeydown = true;　//長押しフラグオン
                            }
                            ReverseLeftRecord();//押している間、レコード逆回転
                            break;

                        case Key.X: // Xキーで左のオーディオを停止
                            if (!isLeftPaused)
                            {
                                StopLeftAudio();
                            }
                            break;

                        case Key.NumPad6: // 6キーで右のオーディオを再生
                            if (rightFilePath != null && !isRightPaused)
                                PlayRightAudio(rightFilePath);
                            else if (isRightPaused)
                                TogglePauseRightAudio();
                            break;

                        case Key.NumPad5: // テンキー5で右のオーディオを一時停止
                            TogglePauseRightAudio();
                            break;

                        case Key.NumPad4: // 4キーで左のオーディオをSB
                            if (!is4Keydown && !isRightPaused)    //4キーが最初に押された時かつ一時停止中でない
                            {
                                rightAudioFile.Volume = 0.0f;　   //ボリュームをゼロにしてミュート
                                right_ts = rightAudioFile.CurrentTime;　   //再生時間取得
                                rightStopwatch.Restart();　   //巻き戻し時間計測用ストップウォッチスタート
                                ScratchBackSound();//スクラッチ音を挿入
                                is4Keydown = true;　 //長押しフラグオン
                            }
                    
                            ReverseRightRecord();    //押している間、レコード逆回転
                            break;

                        case Key.NumPad2: // テンキー2で右のオーディオを停止
                            if (!isRightPaused)
                            {
                                StopRightAudio();
                            }
                            break;
                    }
                }

                // キーアップイベントで回転を通常に戻す
                private void Windows_KeyUp(object sender, KeyEventArgs e)
                {
                    switch (e.Key)
                    {
                        case Key.A:// Aキーで左のオーディオをSB

                            if (isAKeydown)//Aキー開放された時
                            {
                                leftAudioFile.Volume = 1.0f;//ボリュームを通常に戻す
                                leftStopwatch.Stop();   //ストップウォッチ停止
                                TimeSpan adjustedTime = left_ts - (leftStopwatch.Elapsed + leftStopwatch.Elapsed);//再生開始時点を設定　設定で掛率を変更できるといいかもしれない
                                if (adjustedTime < TimeSpan.Zero)
                                {
                                    adjustedTime = TimeSpan.Zero;   //負の時はゼロとする
                                }
                                leftAudioFile.CurrentTime = adjustedTime;//再生再開時間設定
                                leftWavePlayer.Play();//再生再開
                                PauseLeftRecord();//レコード順回転アニメーション再開　一旦止めてから再開
                                ResumeLeftRecord();
                                isAKeydown = false;//フラグオフ
                            }
                            if (!isLeftPaused)
                            {
                                ScratchPlaySound();//レコード再生音挿入
                            }
                            break;

                        case Key.NumPad4:// 4キーで左のオーディオをSB

                            if (is4Keydown)//4キー開放された時
                            {
                                rightAudioFile.Volume = 1.0f;//ボリュームを通常に戻す
                                rightStopwatch.Stop();//ストップウォッチ停止
                                TimeSpan adjustedTime = right_ts - (rightStopwatch.Elapsed + rightStopwatch.Elapsed);//再生開始時点を設定　設定で掛率を変更できるといいかもしれない
                                if (adjustedTime < TimeSpan.Zero)
                                {
                                    adjustedTime = TimeSpan.Zero;//負の時はゼロとする
                                }
                                rightAudioFile.CurrentTime = adjustedTime;//再生再開時間設定
                                rightWavePlayer.Play();  //再生再開
                                PauseRightRecord();//レコード順回転アニメーション再開　一旦止めてから再開
                                ResumeRightRecord();
                                is4Keydown = false;//フラグオフ
                            }
                            if (!isRightPaused)
                            {
                                ScratchPlaySound();//レコード再生音挿入
                            }
                            break;
                    }
                }

                #endregion

    }
}
