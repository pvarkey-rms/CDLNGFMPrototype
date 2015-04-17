using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class HoursClauseWindowGenerator
    {
        private Declarations _contractDeclarations;

        public HoursClauseWindowGenerator(Declarations ContractDeclarations)
        {
            _contractDeclarations = ContractDeclarations;
        }

        #region old code

        //public List<TimeWindow> Generate(int WindowSize, Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> DictionaryLosses)
        //{
        //    //get the unique list of dates from the rites
        //    List<int> lst_dates = new List<int>();
        //    foreach (KeyValuePair<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> l1 in DictionaryLosses)
        //    {
        //        foreach (KeyValuePair<int, Dictionary<long, Tuple<double, uint, List<float>>>> l2 in l1.Value)
        //        {
        //            foreach (KeyValuePair<long, Tuple<double, uint, List<float>>> l3 in l2.Value)
        //            {
        //                if (!lst_dates.Contains((int)l3.Value.Item2))
        //                    lst_dates.Add((int)l3.Value.Item2);
        //            }
        //        }
        //    }
        //    ArrayList arr_dates = new ArrayList(lst_dates);
        //    arr_dates.Sort();
        //    return GenerateWindows(arr_dates, WindowSize);
        //}

        ////assuming dateslist is a sorted list of dates in integer format
        //private List<TimeWindow> GenerateWindows(ArrayList DatesList, int WindowSize)
        //{
        //    double interval = WindowSize + 0.5;
        //    int count = DatesList.Count;
        //    List<TimeWindow> lst_timeWindows = new List<TimeWindow>();

        //    for (int i = 0; i < count; i++)
        //    {
        //        double current = Convert.ToDouble(DatesList[i]);
        //        double lowerlim = Convert.ToDouble(DatesList[i]) - interval;
        //        double upperlim = Convert.ToDouble(DatesList[i]) + interval;
        //        double datediff = 0;

        //        if (i < count - 1)
        //        {
        //            datediff = Convert.ToDouble(DatesList[i + 1]) - Convert.ToDouble(DatesList[i]);
        //        }

        //        TimeWindow leftWindow = new TimeWindow();
        //        leftWindow.SetStartandEnd(lowerlim, current);
        //        lst_timeWindows.Add(leftWindow);
        //        Console.WriteLine("Left Window - (" + leftWindow.start + "," + leftWindow.end + "]");

        //        //to eliminate empty windows and exclude the last window
        //        if (datediff <= interval && i < (count - 1))
        //        {
        //            TimeWindow rightWindow = new TimeWindow();
        //            rightWindow.SetStartandEnd(current, upperlim);
        //            lst_timeWindows.Add(rightWindow);
        //            Console.WriteLine("Right Window - (" + rightWindow.start + "," + rightWindow.end + "]");
        //        }
        //    }
        //    return lst_timeWindows;

        //}

        #endregion

        public List<TimeWindow> Generate(LossTimeSeries SubjectLoss)
        {
            if (_contractDeclarations.IsHoursClause == false || _contractDeclarations.HoursClauses.Count() == 0)                        
                return new List<TimeWindow> { new TimeWindow() };

            List<TimeWindow> resultWindowList = new List<TimeWindow>();
            //List<int> windowsizeList = _contractDeclarations.HoursClauses.Select(x => x.Duration).ToList();
            int windowsize = _contractDeclarations.HoursClauses[0].Duration;
            List<DateTime> dateList = GenerateDateList(SubjectLoss);

            TimeWindow oneWindow = new TimeWindow();            
            if (_contractDeclarations.HoursClauses[0].OnlyOnce) //if only one LO
            {
                return GenerateWindowsOnlyOneLO(dateList, windowsize);
            }
            else
            {
                return GenerateWindows_3(dateList, windowsize);
            }                                      
        }


        public List<DateTime> GenerateDateList(LossTimeSeries SubjectLoss)
        {
            //get the unique list of dates from the rites
            List<DateTime> lst_dates = new List<DateTime>();
            foreach (TimeLoss dt in SubjectLoss)
            {        
                if (!lst_dates.Contains(dt.Time))
                    lst_dates.Add(dt.Time);       
            }
            //ArrayList arr_dates = new ArrayList(lst_dates);
            lst_dates.Sort();
            return lst_dates;
        }

        ////assuming dateslist is a sorted list of dates in integer format
        private List<TimeWindow> GenerateWindowsOnlyOneLO(List<DateTime> DatesList, int WindowSize)
        {
            int interval = WindowSize - 1; 
            int count = DatesList.Count;
            List<TimeWindow> lst_timeWindows = new List<TimeWindow>();

            for (int i = 0; i < count; i++)
            {
                DateTime current = DatesList[i];
                DateTime lowerlim = current.AddDays(-interval);
                DateTime upperlim = current.AddDays(interval);
                double datediff = 0;

                if (i < count - 1)
                {
                    datediff = (DatesList[i + 1] - DatesList[i]).TotalDays;
                }

                TimeWindow leftWindow = new TimeWindow();
                leftWindow.SetStartandEnd(lowerlim, current);
                lst_timeWindows.Add(leftWindow);
                //Console.WriteLine("Left Window - (" + leftWindow.start + "," + leftWindow.end + "]");

                //to eliminate empty windows and exclude the last window
                if (datediff <= interval && i < (count - 1))
                {
                    TimeWindow rightWindow = new TimeWindow();
                    rightWindow.SetStartandEnd(current, upperlim);
                    lst_timeWindows.Add(rightWindow);
                    //Console.WriteLine("Right Window - (" + rightWindow.start + "," + rightWindow.end + "]");
                }
            }
            return lst_timeWindows;
        }

        private List<TimeWindow> GenerateWindows(List<DateTime> DatesList, int WindowSize)
        {
            int count = DatesList.Count;

            List<TimeWindow> lst_timeWindows = new List<TimeWindow>();
            
            DateTime lastStart = new DateTime();

            if (count > 0)
                lastStart = DatesList[count - 1];
        
            for (int i = 0; i < count; i++)
            {
                TimeWindow oneWindow = new TimeWindow();
                DateTime current = DatesList[i];
                DateTime upperlim = DatesList[i].AddDays(WindowSize-1);

                oneWindow.SetStartandEnd(current, upperlim);
                lst_timeWindows.Add(oneWindow);

                //if (upperlim >= lastStart)
                //{
                //    oneWindow.SetStartandEnd(current, upperlim);
                //    lst_timeWindows.Add(oneWindow);
                //    return lst_timeWindows;
                //}
                //else
                //{
                //    oneWindow.SetStartandEnd(current, upperlim);
                //    lst_timeWindows.Add(oneWindow);
                //}
            }

            return lst_timeWindows;
        }


        private List<TimeWindow> GenerateWindows_2(List<DateTime> DatesList, int WindowSize)
        {   //this includes the windows whose start date is not one of the loss date
            int count = DatesList.Count;            
            TimeSpan diff = DatesList[count - 1] - DatesList[0];
            int daysBtwn = diff.Days;

            List<TimeWindow> lst_timeWindows = new List<TimeWindow>();

            DateTime lastStart = new DateTime();

            if (count > 0)
                lastStart = DatesList[count - 1];
            
            for (int i = 0; i <= daysBtwn; i++)
            {
                TimeWindow oneWindow = new TimeWindow();

                DateTime current = DatesList[0].AddDays(i);
                DateTime upperlim = current.AddDays(WindowSize-1);               
                oneWindow.SetStartandEnd(current, upperlim);
                lst_timeWindows.Add(oneWindow);
            }

            return lst_timeWindows;
        }

        private List<TimeWindow> GenerateWindows_3(List<DateTime> DatesList, int WindowSize)
        {   //same logic as Windows_2, but remove the 0 loss windows and duplicate windows
            List<TimeWindow> lst_timeWindows = new List<TimeWindow>();

            int count = DatesList.Count;
            if (count == 0)            
                return lst_timeWindows;
            
            TimeSpan diff = DatesList[count - 1] - DatesList[0];
            int daysBtwn = diff.Days;            

            DateTime lastStart = new DateTime();                           
            lastStart = DatesList[count - 1];

            
            List<DateTime> preWindowDates = new List<DateTime>();

            for (int i = 0; i <= daysBtwn; i++)
            {
                TimeWindow oneWindow = new TimeWindow();

                DateTime current = DatesList[0].AddDays(i);
                DateTime upperlim = current.AddDays(WindowSize - 1);

                List<DateTime> tempDates = new List<DateTime>();

                for (DateTime dt = current; dt <= upperlim; dt = dt.AddDays(1))
                {
                    tempDates.Add(dt);
                }

                List<DateTime> currWindowDates = tempDates.Intersect(DatesList).ToList();
                //List<DateTime> inter1 = currWindowDates.Except(preWindowDates).ToList();
                //List<DateTime> inter2 =  preWindowDates.Except(currWindowDates).ToList();
                //if (currWindowDates.Count() > 0 && (inter1.Count() > 0 || inter2.Count() > 0))
                if (currWindowDates.Count() > 0)
                {
                    oneWindow.SetStartandEnd(current, upperlim);
                    lst_timeWindows.Add(oneWindow);
                }
                preWindowDates = currWindowDates;
            }
            return lst_timeWindows;
        }

    }
}
