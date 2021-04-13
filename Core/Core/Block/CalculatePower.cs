using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;

namespace ETModel
{
    public class CalculatePower
    {
        List<double> diffs = new List<double>();
        private double difftotal = 0L;
        public  double ratio     = 1L;
        public  int    statistic = 4*10;
        public  double diffWhole = 0L; // 全网难度值

        public CalculatePower(double ratio=1L)
        {
            this.ratio = ratio;
        }

        public void Clear()
        {
            diffs.Clear();
            difftotal = 0;
        }

        // 算力统计
        static public void SetDT(Block myblk, Block preblk, HttpPool httpRule)
        {
            if (httpRule == null || myblk == null || preblk == null)
                return;

            Dictionary<string, MinerTask> miners = httpRule?.GetMiner(preblk.height);
            if (miners == null)
                return;

            double difftotal = 0;
            foreach (var miner in miners.Values)
            {
                double.TryParse(miner.power_average, out double diff);
                difftotal += diff;
            }
            if (difftotal != 0)
            {
                if (myblk.extend == null)
                    myblk.extend = new Dictionary<int, string>();
                myblk.extend.Add(1,"" + difftotal);
            }
        }

        public void Insert(Block blk)
        {
            if (blk == null)
                return;

            if (blk.extend != null&& blk.extend.Count>=1)
            {
                double.TryParse(blk.extend[1],out double dt);
                if (dt != 0)
                {
                    InsertPower(dt);
                }
                return;
            }
            diffWhole = blk.GetDiff();
            double power = Power(diffWhole);
            InsertPower(power);
        }
        
        public void InsertLink(Block mcblk, BlockMgr blockMgr)
        {
            if (mcblk == null)
                return;

            diffWhole = 0;

            for (int ii = 0; ii < mcblk.linksblk.Count; ii++)
            {
                Block blk = blockMgr.GetBlock(mcblk.linksblk[ii]);
                if (blk!=null&&blk.extend!=null && blk.extend.Count >= 1)
                {
                    double.TryParse(blk.extend[1], out double dt);
                    diffWhole += dt;
                }
            }

            InsertPower(diffWhole);
        }

        public static double Power(double tempdiff)
        {
            string str = tempdiff.ToString("N16");
            str = str.Replace("0.", "");
            while (str.Length < 16)
            {
                str = str + "0";
            }

            double power = 1;
            int ii = 0;
            for (; ii < str.Length; ii++)
            {
                double value1 = double.Parse("" + str[ii]);
                double value2 = value1 / (10 - value1);
                if (value2>1f)
                    power = power * value2;

                if (value1!=9)
                    break;
            }

            var acc = str.Substring(ii+1,Math.Min(4, str.Length-(ii + 1)));
            double.TryParse(acc,out double accd);

            power = power + accd;

            return power;
        }

        public void InsertPower(double power)
        {
            if (power == 0)
                return ;
            //if (difftotal != 0)
            //{
            //    power = Math.Min(difftotal * 10, power);
            //    power = Math.Max(difftotal * 0.1, power);
            //}
            diffs.Add(power);

            if (diffs.Count > statistic) // 
                diffs.RemoveAt(0);

            difftotal = 0;
            for (int i = 0; i < diffs.Count; i++)
            {
                difftotal = difftotal + diffs[i];
            }
            difftotal = difftotal / diffs.Count;
        }

        public double GetPowerDouble()
        {
            return difftotal*ratio;
        }

        public string GetPower()
        {
            return GetPowerCompany(difftotal*ratio);
        }

        public static string GetPowerCompany(double power)
        {
            int place = 0;
            double value = power;
            while ((value / 1000) > 1)
            {
                value = value / 1000;
                place = place + 1;
            }

            String company = "";
            if (place == 1)
                company = "K";
            else
            if (place == 2)
                company = "M";
            else
            if (place == 3)
                company = "G";
            else
            if (place == 4)
                company = "T";
            else
            if (place == 5)
                company = "P";
            else
            if (place == 6)
                company = "E";

            return string.Format("{0:N2}{1}", value, company);
        }


        static public void Test()
        {
            //
            {
                Block blk = new Block();
                blk.prehash = "b6b67d3d8b83f4885620ccd45d1af81d5690a056de2aba8ddf899fba8088b75d";
                {
                    blk.hash = "000000c09983d8950d8d0dce9ab5e9039fade2590d25f361ee0f9c1047832ceb";
                    double value1 = CalculatePower.Power(blk.GetDiff());
                    string value2 = CalculatePower.GetPowerCompany(value1);
                    Log.Info($"\n PowerCompany {blk.GetDiff()} \n {value2} \n {blk.hash}");
                }
                {
                    blk.hash = "0000003e833bc8b524922d9400e8b489eb1fe753d35efde0d5e2ae5ee430dfbe";
                    double value1 = CalculatePower.Power(blk.GetDiff());
                    string value2 = CalculatePower.GetPowerCompany(value1);
                    Log.Info($"\n PowerCompany {blk.GetDiff()} \n {value2} \n {blk.hash}");
                }
            }
            // 2
            {
                CalculatePower calculate = new CalculatePower();

                Block blk = new Block();
                blk.prehash = "b6b67d3d8b83f4885620ccd45d1af81d5690a056de2aba8ddf899fba8088b75d";

                double diff_max = 0;
                for (int jj = 0; jj < 100; jj++)
                {
                    for (int ii = 0; ii < 1000 * 1000; ii++)
                    {
                        string random = RandomHelper.RandUInt64().ToString("x");
                        string hash = blk.ToHash(random);

                        double diff = Helper.GetDiff(hash);
                        if (diff > diff_max)
                        {
                            diff_max = diff;
                            blk.hash = hash;
                            blk.random = random;
                        }
                    }

                    double value1 = CalculatePower.Power(blk.GetDiff());
                    string value2 = CalculatePower.GetPowerCompany(value1);

                    Log.Info($"\n PowerCompany {blk.GetDiff()} \n {value2} \n {blk.hash}");
                }
            }
        }

    }

}
