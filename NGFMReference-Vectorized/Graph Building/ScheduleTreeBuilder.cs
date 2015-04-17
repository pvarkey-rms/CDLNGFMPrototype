using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class ScheduleTreeBuilder
    {
        public long ContractID { get; private set; }
        public List<IScheduleInput> OriginalSchedules {get; private set;}
        public List<IScheduleInput> WholeSchedules { get; set; }
        public Dictionary<string, Dictionary<string, ScheduleCompareOutcome>> ScheduleTree {get; private set;}
        public Dictionary<IScheduleInput, HashSet<IScheduleInput>> PerRiskExplosionMapping {get; private set;}

        public ScheduleTreeBuilder(List<IScheduleInput> _inputList)
        {
            OriginalSchedules = new List<IScheduleInput>(_inputList);
            WholeSchedules = new List<IScheduleInput>(_inputList);
            ScheduleTree = new Dictionary<string, Dictionary<string, ScheduleCompareOutcome>>();
            PerRiskExplosionMapping = new Dictionary<IScheduleInput, HashSet<IScheduleInput>>();
        }

        public void Run()
        {
            BuildInitialTree();
            UpdateGraphForPerRisk();            
        }

        public void BuildInitialTree()
        {
            GrowScheduleTree(OriginalSchedules, OriginalSchedules);            
        }

        public void GrowScheduleTree(List<IScheduleInput> scheduleList1, List<IScheduleInput> scheduleList2)
        {
            foreach (IScheduleInput schedule1 in scheduleList1)
            {
                foreach (IScheduleInput schedule2 in scheduleList2)
                {
                    if (ScheduleTree.ContainsKey(schedule1.ScheduleName) && ScheduleTree[schedule1.ScheduleName].ContainsKey(schedule2.ScheduleName))
                    {
                        continue;
                    }
                    else
                    {
                        ScheduleCompareOutcome result = CompareTwoSchedules(schedule1, schedule2);
                        ScheduleCompareOutcome resultOpposite;
                        if (result == ScheduleCompareOutcome.Overlap || result == ScheduleCompareOutcome.Disjoin || result == ScheduleCompareOutcome.Equal)
                            resultOpposite = result;
                        else if (result == ScheduleCompareOutcome.Child)
                            resultOpposite = ScheduleCompareOutcome.Parent;
                        else if (result == ScheduleCompareOutcome.Parent)
                            resultOpposite = ScheduleCompareOutcome.Child;
                        else
                            throw new NotSupportedException();

                        if (!ScheduleTree.ContainsKey(schedule1.ScheduleName)) 
                            ScheduleTree.Add(schedule1.ScheduleName, new Dictionary<string, ScheduleCompareOutcome>() { { schedule2.ScheduleName, result } });
                        else if (!ScheduleTree[schedule1.ScheduleName].ContainsKey(schedule2.ScheduleName))
                            ScheduleTree[schedule1.ScheduleName].Add(schedule2.ScheduleName, result);

                        if (!ScheduleTree.ContainsKey(schedule2.ScheduleName))
                            ScheduleTree.Add(schedule2.ScheduleName, new Dictionary<string, ScheduleCompareOutcome>() { { schedule1.ScheduleName, resultOpposite } });
                        else if (!ScheduleTree[schedule2.ScheduleName].ContainsKey(schedule1.ScheduleName))
                            ScheduleTree[schedule2.ScheduleName].Add(schedule1.ScheduleName, resultOpposite);
                    }
                }
            }        
        }

        public void UpdateGraphForPerRisk()
        {
            List<IScheduleInput> newSchedules = new List<IScheduleInput>();
            bool existed;
            List<IScheduleInput> CopyOfOriginalSchedules = new List<IScheduleInput>(OriginalSchedules);
            List<IScheduleInput> PerRiskScheduleList = new List<IScheduleInput>();
            HashSet<RITE> ExplodedRiteSet = new HashSet<RITE>();
            List<IScheduleInput> ExplodedRiteScheduleSet = new List<IScheduleInput>();

            foreach (IScheduleInput schedule in CopyOfOriginalSchedules)
            {
                if (schedule.IsPerRisk)
                {
                    PerRiskScheduleList.Add(schedule);
                    ScheduleTree.Remove(schedule.ScheduleName); 
                    //TODO: also remove the schedule from the second key
                    OriginalSchedules.Remove(schedule);
                    ExplodedRiteSet.UnionWith(schedule.GetScheduleRITEList());
                }
            }

            foreach (RITE aRite in ExplodedRiteSet)
            {
                IScheduleInput newS = FormScheduleFromARITE(aRite, out existed);
                if (!existed)
                    ExplodedRiteScheduleSet.Add(newS);
            }

            GrowScheduleTree(ExplodedRiteScheduleSet, OriginalSchedules);
                
        }

        public ScheduleInput FormScheduleFromARITE(RITE aRite, out bool existed)
        {
            ScheduleInput sInput = null;
            existed = false;

            HashSet<RITE> riteSet = new HashSet<RITE>() { aRite };
            //check if already exists
            foreach (IScheduleInput schedule in OriginalSchedules)
            {
                if (schedule.NumOfRites == 1 && schedule.GetScheduleRITEList().SetEquals(riteSet))
                {
                    existed = true;
                    break;
                }
            }
                
            if (!existed)
            {
                string sName = "exploded." + aRite.ExposureID;               
                ScheduleOfRITEs sOfRite = new ScheduleOfRITEs(sName, riteSet, aRite.RiskCharacteristics);
                sInput = new ScheduleInput(sOfRite);
                WholeSchedules.Add(sInput);
            }           
            return sInput;
        }

        public ScheduleInput FormScheduleFromARITE2(RITE aRite)
        {
  
                string sName = "exploded." + aRite.ExposureID;
                HashSet<RITE> riteSet = new HashSet<RITE>() { aRite };
                ScheduleOfRITEs sOfRite = new ScheduleOfRITEs(sName, riteSet, aRite.RiskCharacteristics);
                ScheduleInput sInput = new ScheduleInput(sOfRite);
                   
            return sInput;
        }

        public ScheduleCompareOutcome CompareTwoSchedules(IScheduleInput s1, IScheduleInput s2)
        {
            HashSet<RITE> list1 = s1.GetScheduleRITEList();
            HashSet<RITE> list2 = s2.GetScheduleRITEList();

            if (list1.Count == list2.Count && list1.IsSubsetOf(list2))
            {

                if (s1.IsPerRisk == s2.IsPerRisk)
                    return ScheduleCompareOutcome.Equal;
                else if (s1.IsPerRisk && !s2.IsPerRisk)
                    return ScheduleCompareOutcome.Parent;
                else
                    return ScheduleCompareOutcome.Child;
            }
            else if (list1.Count < list2.Count && list1.IsProperSubsetOf(list2))
            {                           
                return ScheduleCompareOutcome.Parent;              
            }
            else if (list1.Count > list2.Count && list1.IsProperSupersetOf(list2))
            {
                 return ScheduleCompareOutcome.Child;               
            }
            else if (list1.Overlaps(list2))
            {
                return ScheduleCompareOutcome.Overlap;
            }
            else
            {
                return ScheduleCompareOutcome.Disjoin;
            }            
        }

    }
}
