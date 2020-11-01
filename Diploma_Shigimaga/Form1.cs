using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using static System.Math;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Accord.MachineLearning;

namespace Diploma_Shigimaga
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        double[] file_arr = new double[0];
        double[] narr = new double[0];
        double[,] fft_arr = new double[0, 0];
        double[,] mfcc_arr = new double[0, 0];
        double[][] train_data = new double[0][];
        double[][] train_mfcc = new double[0][];
        double[][] train_emfcc = new double[0][];
        double[] mfcc_earr = new double[0];
        double[][] train_clasdata = new double[0][];
        int min = 0;
        bool predClass;
        int answer;

        private void Form1_Load(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Application.StartupPath + "\\SOUND";
        }

        private void btn_open_Click(object sender, EventArgs e)
        {
            DownloadFile();
        }

        public void DownloadFile()
        {
            
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string path = openFileDialog1.FileName;
                lbl_file.Text = Path.GetFileName(path);
                FileStream fstream = File.OpenRead(path);

                int p = Convert.ToInt32((fstream.Length - 44.0) / 2.0);

                file_arr = new double[p];
                fstream.Close();

                using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    reader.ReadBytes(24);
                    //sampleRate = reader.ReadInt32();
                    reader.ReadBytes(16);
                    //reader.ReadBytes(44);

                    for (int i = 0; i < p; i++)
                    {
                        file_arr[i] = reader.ReadInt16();
                    }
                }

                GetNormalArr(file_arr, out narr, 128.0);

                //dataGridView1.RowCount = narr.Length;
                //dataGridView1.ColumnCount = 1;
                chart1.Series[0].Points.Clear();
                chart3.Series[0].Points.Clear();

                for (int i = 0; i <= file_arr.Length - 1; i++)
                {
                    //dataGridView1.Rows[i].Cells[0].Value = narr[i];
                    chart1.Series[0].Points.AddXY(i + 1, file_arr[i]);
                }

                for (int i = 0; i <= narr.Length - 1; i++)
                {
                    //dataGridView1.Rows[i].Cells[0].Value = narr[i];
                    chart3.Series[0].Points.AddXY(i + 1, narr[i]);
                }

                GetMFCC(narr, out mfcc_arr);

                dataGridView1.RowCount = mfcc_arr.GetLength(0);
                dataGridView1.ColumnCount = mfcc_arr.GetLength(1);
                for (int i = 0; i < mfcc_arr.GetLength(0); i++)
                    for (int j = 0; j < mfcc_arr.GetLength(1); j++)
                        dataGridView1.Rows[i].Cells[j].Value = Round(mfcc_arr[i, j], 4);
            }
        }

        public void Framing(double[] arr1, out double[,] arr2)
        {
            int m = Convert.ToInt32(arr1.Length / 128.0);
            arr2 = new double[m, 256];
            int h = 0;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    if (j + h >= arr1.Length)
                        break;
                    arr2[i, j] = arr1[j + h];
                }
                h += 128;
            }

        }

        public void HammingWindow(double[,] arr)
        {
            //arr2 = new double[arr1.GetLength(0), arr1.GetLength(1)];
            for (int i = 0; i < arr.GetLength(0); i++)
                for (int j = 0; j < arr.GetLength(1); j++)
                    arr[i, j] = 0.54 - 0.46 * Cos(2 * PI * j / arr.GetLength(1));

        }

        public void FourierExp(double[,] arr1, out double[,] arr2, int m)
        {
            arr2 = new double[m, 256];

            for (int p = 0; p <= m - 1; p++)
            {

                for (int k = 1; k <= 256; k++)
                {
                    double a = 0, b = 0, c = 0, sum = 0;

                    for (int i = 0; i <= 255; i++)
                        sum += arr1[p, i] * Sin((2.0 * PI * k * (p * 256 + i)) / 256.0);
                    a = 2.0 / 256.0 * sum;

                    sum = 0;

                    for (int i = 0; i <= 255; i++)
                        sum += arr1[p, i] * Cos((2.0 * PI * k * (p * 256 + i)) / 256.0);
                    b = 2.0 / 256.0 * sum;

                    c = Pow(a, 2) + Pow(b, 2);

                    arr2[p, k - 1] = c;

                }
            }
        }

        public void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            chart2.Series[0].Points.Clear();
            trackBar1.Maximum = fft_arr.GetLength(0);
            BuildChrt1(fft_arr, trackBar1.Value, 128);
        }

        public void BuildChrt1(double[,] arr, int m, int n)
        {
            chart2.Series[0].Points.Clear();


            for (int i = 0; i <= n - 1; i++)
            {
                chart2.Series[0].Points.AddXY(i + 1, arr[m - 1, i]);
            }
        }

        public double Dispersion(double[] arr)
        {
            double sum = 0, d = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                sum += Math.Pow(arr[i], 2);
            }

            d = Math.Sqrt(sum / arr.Length);
            return d;
        }

        public void GetNormalArr(double[] arr1, out double[] arr2, double k)
        {
            double[] temp = new double[0];
            double u = (1.0 / 3.0 - 1.0 / 5.0) * Dispersion(arr1);
            int start = 0, end = 0;
            for (int i = 20; i < arr1.Length; i++)  //начинаем с 20, потому что до этого идут аномально високие частоты,
                                                    //вероятно, щелчок мыши перед записью
            {
                if (arr1[i] >= u)
                {
                    start = i;
                    break;
                }
            }

            for (int i = arr1.Length - 1; i >= 0; i--)
            {
                if (arr1[i] >= u)
                {
                    end = arr1.Length - 1 - i;
                    break;
                }
            }

            int length = arr1.Length - start - end;

            temp = new double[length];
            for (int i = 0; i < length; i++)
            {
                temp[i] = arr1[i + start];
            }
            double d = Dispersion(temp);
            for (int i = 0; i < temp.Length; i++)
                temp[i] = temp[i] / d;


            int true_length = length;
            if (length % k != 0)
                true_length = Convert.ToInt32(Math.Truncate((length / k + 1.0)) * k);
            arr2 = new double[true_length];
            Array.Copy(temp, arr2, length);
        }

        public void ApplyingFilters(double[,] arr1, out double[,] arr2, int M)
        {
            arr2 = new double[arr1.GetLength(0), M];
            double[] f = { 4, 8, 12, 17, 23, 31, 40, 52, 66, 82, 103, 128 };

            for (int i = 0; i < arr2.GetLength(0); i++)
            {
                for (int m = 1; m <= arr2.GetLength(1); m++)
                {
                    double sum = 0;
                    for (int k = 1; k <= 128; k++)
                    {
                        double h = 0;

                        if ((k >= f[m - 1]) && (k <= f[m]))
                            h = (k - f[m - 1]) / (f[m] - f[m - 1]);
                        else if ((k >= f[m]) && (k <= f[m + 1]))
                            h = (f[m + 1] - k) / (f[m + 1] - f[m]);

                        sum += arr1[i, k - 1] * h;
                    }
                    arr2[i, m - 1] = Log(sum);

                }
            }
        }

        public void CosineTransform(double[,] arr1, out double[,] arr2, int M)
        {
            arr2 = new double[arr1.GetLength(0), arr1.GetLength(1)];

            for (int i = 0; i < arr2.GetLength(0); i++)
                for (int l = 0; l < arr2.GetLength(1); l++)
                {
                    double sum = 0;

                    for (int m = 0; m < arr2.GetLength(1); m++)
                        sum += arr1[i, m] * Cos(PI * l * (m + 1.0 / 2.0) / Convert.ToDouble(M));
                    arr2[i, l] = sum;
                }
        }

        public void GetMFCC(double[] narr, out double[,] mfcc_arr)
        {
            //int sampleRate;
            double[,] framed_arr = new double[0, 0];
            double[,] sm_arr = new double[0, 0];

            Framing(narr, out framed_arr);
            FourierExp(framed_arr, out fft_arr, framed_arr.GetLength(0));
            BuildChrt1(fft_arr, trackBar1.Value, 128);
            ApplyingFilters(fft_arr, out sm_arr, 10);
            CosineTransform(sm_arr, out mfcc_arr, 10);

        }

        public void GetTrainMFCC()
        {
            string path = Application.StartupPath + "\\SOUND\\TrainData";
            int count = Directory.GetFiles(path, "*.wav*", SearchOption.AllDirectories).Length;
            train_data = new double[count][];
            train_mfcc = new double[count][];
            int iter = 0;
            foreach (var file in Directory.EnumerateFiles(path, "*.wav*", SearchOption.AllDirectories))
            {
                FileStream fstream = File.OpenRead(file);

                int p = Convert.ToInt32((fstream.Length - 44.0) / 2.0);

                double[] train_arr = new double[p];
                fstream.Close();

                using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open)))
                {
                    reader.ReadBytes(24);
                    //sampleRate = reader.ReadInt32();
                    reader.ReadBytes(16);
                    //reader.ReadBytes(44);

                    for (int i = 0; i < p; i++)
                    {
                        train_arr[i] = reader.ReadInt16();
                    }
                }

                double[] train_narr = new double[0];
                GetNormalArr(train_arr, out train_narr, 128.0);

                train_data[iter] = new double[train_arr.Length];
                for (int i = 0; i < train_arr.Length; i++)
                        train_data[iter][i] = train_arr[i];

                double[,] framed_tarr = new double[0, 0];
                double[,] fft_tarr = new double[0, 0];
                double[,] sm_tarr = new double[0, 0];
                double[,] mfcc_tarr = new double[0, 0];

                Framing(train_narr, out framed_tarr);
                FourierExp(framed_tarr, out fft_tarr, framed_tarr.GetLength(0));
                ApplyingFilters(fft_tarr, out sm_tarr, 10);
                CosineTransform(sm_tarr, out mfcc_tarr, 10);

                train_mfcc[iter] = new double[mfcc_tarr.Length];
                for (int i = 0; i < mfcc_tarr.GetLength(0); i++)
                    for (int j = 0; j < mfcc_tarr.GetLength(1); j++)
                    {
                        train_mfcc[iter][i * mfcc_tarr.GetLength(1) + j] = mfcc_tarr[i, j];
                    }
                iter++;
            }
            EditMFCC();
        }

        public void EditMFCC()
        {
            min = mfcc_arr.Length;
            for (int i = 0; i < train_mfcc.Length; i++)
            {
                if (train_mfcc[i].Length < min)
                    min = train_mfcc[i].Length;
            }

            train_emfcc = new double[train_mfcc.Length][];

            for (int i = 0; i < train_emfcc.Length; i++)
            {
                train_emfcc[i] = new double[min];
                for (int j = 0; j < train_emfcc[i].Length; j++)
                    train_emfcc[i][j] = train_mfcc[i][j];
            }

            mfcc_earr = new double[min];
            for (int i = 0; i < min / mfcc_arr.GetLength(1); i++)
                for (int j = 0; j < mfcc_arr.GetLength(1); j++)
                {
                    mfcc_earr[i * mfcc_arr.GetLength(1) + j] = mfcc_arr[i, j];
                }
        }

        public void SupportVectorMachine(out bool predClass)
        {
            bool[] preds;
            double[] score;
            int[] y = { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                        1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            if (radioButton1.Checked)
            {
                var smo = new SequentialMinimalOptimization<Linear>();
                var svm = smo.Learn(train_emfcc, y);
                preds = svm.Decide(train_emfcc);
                score = svm.Score(train_emfcc);
                predClass = svm.Decide(mfcc_earr);

            }
            else if (radioButton2.Checked)
            {
                var smo = new SequentialMinimalOptimization<Polynomial>();
                var svm = smo.Learn(train_emfcc, y);
                preds = svm.Decide(train_emfcc);
                score = svm.Score(train_emfcc);
                predClass = svm.Decide(mfcc_earr);
            }
            else
            {
                var smo = new SequentialMinimalOptimization<Gaussian>();
                var svm = smo.Learn(train_emfcc, y);
                preds = svm.Decide(train_emfcc);
                score = svm.Score(train_emfcc);
                predClass = svm.Decide(mfcc_earr);
            }

            dataGridView2.RowCount = score.Length;
            dataGridView2.ColumnCount = 2;
            for (int i = 0; i < score.Length; i++)
            {
                dataGridView2.Rows[i].Cells[0].Value = preds[i];
                dataGridView2.Rows[i].Cells[1].Value = Round(score[i], 4);
            }

            label8.Text += predClass.ToString();

        }


        public void KNearestNeighbors(out int answer)
        {
            int[] y = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                        1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            int kn = Convert.ToInt32(textBox1.Text);
            var knn = new KNearestNeighbors(k: kn);
            knn.Learn(train_emfcc, y);
            answer = knn.Decide(mfcc_earr);

            label6.Text += answer.ToString();
        }

        public void FourierExp(double[] arr1, double[,] arr2, int m)
        {

            for (int p = 0; p <= m - 1; p++)
            {

                for (int k = 1; k <= 256; k++)
                {
                    double a = 0, b = 0, c = 0, sum = 0;

                    for (int i = 0; i <= 255; i++)
                        sum += arr1[p * 256 + i] * Math.Sin((2.0 * Math.PI * k * (p * 256 + i)) / 256.0);
                    a = 2.0 / 256.0 * sum;

                    sum = 0;

                    for (int i = 0; i <= 255; i++)
                        sum += arr1[p * 256 + i] * Math.Cos((2.0 * Math.PI * k * (p * 256 + i)) / 256.0);
                    b = 2.0 / 256.0 * sum;

                    c = Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));

                    arr2[p, k - 1] = c;

                }
            }
        }

        public void ChebyshevExp(double[] arr1, double[,] arr2, int m)
        {
            for (int p = 0; p <= m - 1; p++)
            {

                for (int k = 1; k <= 256; k++)
                {
                    double c = 0, sum = 0;
                    if (k == 1)
                    {
                        for (int i = 0; i <= 255; i++)
                            sum += arr1[p * 256 + i];
                        c = Math.Sqrt(1.0 / 256.0) * sum;
                    }
                    else
                    {
                        for (int i = 0; i <= 255; i++)
                            sum += arr1[p * 256 + i] * Math.Cos(Math.PI * k * (i + 0.5) / 256.0);
                        c = Math.Sqrt(2.0 / 256.0) * sum;
                    }
                    arr2[p, k - 1] = c;
                }
            }
        }
        public void Transpose(double[,] arr1, double[,] arr2, int m)
        {
            for (int i = 0; i < 256; i++)
                for (int j = 0; j < m; j++)
                {
                    arr2[i, j] = arr1[j, i];

                }
        }

        public void SpectralBandRepr(double[,] arr1, double[,] arr2, int m)
        {
            for (int i = 0; i < m; i++) //1 полоса
                arr2[0, i] = Math.Sqrt(Math.Pow(arr1[0, i], 2) + Math.Pow(arr1[1, i], 2));

            for (int i = 0; i < m; i++) //2 полоса
                arr2[1, i] = Math.Sqrt(Math.Pow(arr1[2, i], 2) + Math.Pow(arr1[3, i], 2));

            for (int i = 0; i < m; i++) //3 полоса
                arr2[2, i] = Math.Sqrt(Math.Pow(arr1[4, i], 2) + Math.Pow(arr1[5, i], 2));

            for (int i = 0; i < m; i++) //4 полоса
                arr2[3, i] = Math.Sqrt(Math.Pow(arr1[6, i], 2) + Math.Pow(arr1[7, i], 2));

            for (int i = 0; i < m; i++) //5 полоса
                arr2[4, i] = Math.Sqrt(Math.Pow(arr1[8, i], 2) + Math.Pow(arr1[9, i], 2));

            for (int i = 0; i < m; i++) //6 полоса
            {
                double sum = 0;
                for (int t = 10; t <= 14; t++)
                    sum += Math.Pow(arr1[t, i], 2);
                arr2[5, i] = Math.Sqrt(sum);
            }

            for (int i = 0; i < m; i++) //7 полоса
            {
                double sum = 0;
                for (int t = 15; t <= 24; t++)
                    sum += Math.Pow(arr1[t, i], 2);
                arr2[6, i] = Math.Sqrt(sum);
            }

            for (int i = 0; i < m; i++) //8 полоса
            {
                double sum = 0;
                for (int t = 25; t <= 49; t++)
                    sum += Math.Pow(arr1[t, i], 2);
                arr2[7, i] = Math.Sqrt(sum);
            }

            for (int i = 0; i < m; i++) //9 полоса
            {
                double sum = 0;
                for (int t = 50; t <= 127; t++)
                    sum += Math.Pow(arr1[t, i], 2);
                arr2[8, i] = Math.Sqrt(sum);
            }

        }

        public void MatrixDis(double[,] arr1, double[,] arr2, double[,] arr3, int m, int n)
        {
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                {
                    arr3[i, j] = Math.Sqrt(Math.Pow((arr1[0, i] - arr2[0, j]), 2) +
                                           Math.Pow((arr1[1, i] - arr2[1, j]), 2) +
                                           Math.Pow((arr1[2, i] - arr2[2, j]), 2) +
                                           Math.Pow((arr1[3, i] - arr2[3, j]), 2) +
                                           Math.Pow((arr1[4, i] - arr2[4, j]), 2) +
                                           Math.Pow((arr1[5, i] - arr2[5, j]), 2) +
                                           Math.Pow((arr1[6, i] - arr2[6, j]), 2) +
                                           Math.Pow((arr1[7, i] - arr2[7, j]), 2) +
                                           Math.Pow((arr1[8, i] - arr2[8, j]), 2));
                }
        }

        public void MatrixDef(double[,] arr1, double[,] arr2, int m, int n)
        {
            arr2[0, 0] = arr1[0, 0];


            for (int i = 1; i < m; i++)
                arr2[i, 0] = arr1[i, 0] + arr1[i - 1, 0];

            for (int j = 1; j < n; j++)
                arr2[0, j] = arr1[0, j] + arr1[0, j - 1];

            for (int i = 1; i < m; i++)
                for (int j = 1; j < n; j++)
                    arr2[i, j] = arr1[i, j] + MinOf3(arr1[i - 1, j], arr1[i - 1, j - 1], arr1[i, j - 1]);


        }

        public double MinOf3(double n1, double n2, double n3)
        {
            double min = n1;

            if (n2 < min)
                min = n2;

            if (n3 < min)
                min = n3;

            return min;
        }

        public double FindDTW(double[,] arr, int m, int n)
        {
            double sumw = 0;
            int i = m - 1, j = n - 1, wcount = 0;

            do
            {
                if (i > 0 && j > 0)
                    if (arr[i - 1, j - 1] <= arr[i - 1, j])
                        if (arr[i - 1, j - 1] <= arr[i, j - 1]) { i--; j--; } else j--;
                    else
                        if (arr[i - 1, j] <= arr[i, j - 1]) i--; else j--;
                else if (i == 0) j--; else i--;
                sumw += arr[i, j];
                wcount++;
            }
            while (i != 0 || j != 0);


            return sumw / wcount;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GetTrainMFCC();
            SupportVectorMachine(out predClass);
            KNearestNeighbors(out answer);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
            {
                if (predClass == false)
                {
                    train_clasdata = new double[10][];
                    for (int i = 0; i < train_clasdata.Length; i++)
                    {
                        train_clasdata[i] = new double[train_data[i].Length];
                        for (int j = 0; j < train_clasdata[i].Length; j++)
                            train_clasdata[i][j] = train_data[i][j];
                    }
                }
                else
                {
                    train_clasdata = new double[10][];
                    for (int i = 0; i < train_clasdata.Length; i++)
                    {
                        train_clasdata[i] = new double[train_data[i + 10].Length];
                        for (int j = 0; j < train_clasdata[i].Length; j++)
                        train_clasdata[i][j] = train_data[i + 10][j];
                    }
                }
            }
            else
                if (answer == 0)
            {
                train_clasdata = new double[10][];
                for (int i = 0; i < train_clasdata.Length; i++)
                {
                    train_clasdata[i] = new double[train_data[i].Length];
                    for (int j = 0; j < train_clasdata[i].Length; j++)
                        train_clasdata[i][j] = train_data[i][j];
                }
            }
            else
            {
                train_clasdata = new double[10][];
                for (int i = 0; i < train_clasdata.Length; i++)
                {
                    train_clasdata[i] = new double[train_data[i + 10].Length];
                    for (int j = 0; j < train_clasdata[i].Length; j++)
                        train_clasdata[i][j] = train_data[i + 10][j];
                }
            }

            double w_c_min = 1000000;
            double w_min = 1000000;
            int i_min = 0;
            int i_c_min = 0;

            double[] narr1 = new double[0];
            double[,] arr1c = new double[0, 0]; 
            double[,] arr1cT = new double[0, 0]; 
            double[,] sbarr1 = new double[9, 0];
            double[,] defmc = new double[0, 0];
            double[,] defm = new double[0, 0];

            GetNormalArr(file_arr, out narr1, 256.0);
            int s1 = Convert.ToInt32(Ceiling(narr1.Length / 256.0));
            arr1c = new double[s1, 256];
            FourierExp(narr1, arr1c, s1);
            arr1cT = new double[256, s1];
            Transpose(arr1c, arr1cT, s1);
            sbarr1 = new double[9, s1];
            SpectralBandRepr(arr1cT, sbarr1, s1);

            for (int iter = 0; iter < train_clasdata.Length; iter++)
            {
                double[] narr2 = new double[0];
                double[,] arr2c = new double[0, 0];
                double[,] arr2cT = new double[0, 0];
                double[,] sbarr2 = new double[9, 0];

                GetNormalArr(train_clasdata[iter], out narr2, 256.0);
                int s2 = Convert.ToInt32(Ceiling(narr2.Length / 256.0));
                arr2c = new double[s2, 256];
                FourierExp(narr2, arr2c, s2);
                arr2cT = new double[256, s2];
                Transpose(arr2c, arr2cT, s2);
                sbarr2 = new double[9, s2];
                SpectralBandRepr(arr2cT, sbarr2, s2);

                int m = sbarr1.Length / 9;
                int n = sbarr2.Length / 9;
                double[,] dism = new double[m, n];
                defmc = new double[m, n];

                MatrixDis(sbarr1, sbarr2, dism, m, n);
                MatrixDef(dism, defmc, m, n);
               double w = FindDTW(defmc, m, n);
               if (w < w_c_min)
                {
                    w_c_min = w;
                    i_c_min = iter;

                    dataGridView3.RowCount = defmc.GetLength(0);
                    dataGridView3.ColumnCount = defmc.GetLength(1);
                    for (int i = 0; i < defmc.GetLength(0); i++)
                        for (int j = 0; j < defmc.GetLength(1); j++)
                            dataGridView3.Rows[i].Cells[j].Value = Round(defmc[i, j], 4);
                }
            }

            label9.Text += w_c_min.ToString();

            for (int iter = 0; iter < train_data.Length; iter++)
            {
                double[] narr2 = new double[0];
                double[,] arr2c = new double[0, 0];
                double[,] arr2cT = new double[0, 0];
                double[,] sbarr2 = new double[9, 0];

                GetNormalArr(train_data[iter], out narr2, 256.0);
                int s2 = Convert.ToInt32(Ceiling(narr2.Length / 256.0));
                arr2c = new double[s2, 256];
                FourierExp(narr2, arr2c, s2);
                arr2cT = new double[256, s2];
                Transpose(arr2c, arr2cT, s2);
                sbarr2 = new double[9, s2];
                SpectralBandRepr(arr2cT, sbarr2, s2);

                int m = sbarr1.Length / 9;
                int n = sbarr2.Length / 9;
                double[,] dism = new double[m, n];
                defm = new double[m, n];

                MatrixDis(sbarr1, sbarr2, dism, m, n);
                MatrixDef(dism, defm, m, n);
                double w = FindDTW(defm, m, n);
                if (w < w_min)
                {
                    w_min = w;
                    i_min = iter;

                    dataGridView4.RowCount = defm.GetLength(0);
                    dataGridView4.ColumnCount = defm.GetLength(1);
                    for (int i = 0; i < defm.GetLength(0); i++)
                        for (int j = 0; j < defm.GetLength(1); j++)
                            dataGridView4.Rows[i].Cells[j].Value = Round(defm[i, j], 4);
                }
            }

            label10.Text += w_min.ToString();

        }
    }
}
