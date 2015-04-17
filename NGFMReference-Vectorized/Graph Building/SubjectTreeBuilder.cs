using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class SubjectTreeBuilder
    {
        public long ContractID { get; private set; }
        public bool MatrixForm { get; set; }
        public HashSet<PrimarySubject> OriginalSubjects { get; private set; }
        public HashSet<PrimarySubject> OriginalSubjectsMinusPerRisk { get; private set; }
        public HashSet<PrimarySubject> WholeSubjects { get; set; }
        public HashSet<PrimarySubject> OriginalPerRiskSubjects { get; private set; }
        public HashSet<PrimarySubject> ExplodedSubjectList { get; private set; }
        public SubjectCompareOutcome[,] SubjectTreeMatrix { get; private set; }
        public Dictionary<int, SubjectCompareOutcome>[] SubjectTreeArray { get; private set; }

        //public Dictionary<string, Dictionary<string, SubjectCompareOutcome>> SubjectTree { get; private set; }
        //public Dictionary<ISubjectInput, HashSet<ISubjectInput>> PerRiskExplosionMapping { get; private set; }

        public SubjectTreeBuilder(HashSet<PrimarySubject> _inputList, bool _matrixForm)
        {
            OriginalSubjects = new HashSet<PrimarySubject>(_inputList);
            WholeSubjects = new HashSet<PrimarySubject>(_inputList);
            OriginalPerRiskSubjects = new HashSet<PrimarySubject>();
            foreach (PrimarySubject subject in OriginalSubjects)
            {
                if (subject.IsPerRisk)
                    OriginalPerRiskSubjects.Add(subject);
            }
            ExplodedSubjectList = new HashSet<PrimarySubject>();
            MatrixForm = _matrixForm;
            if (MatrixForm)
                SubjectTreeMatrix = new SubjectCompareOutcome[21000, 21000];
            else
                SubjectTreeArray = new Dictionary<int, SubjectCompareOutcome>[1000000];

            OriginalSubjectsMinusPerRisk = new HashSet<PrimarySubject>(OriginalSubjects);
            OriginalSubjectsMinusPerRisk.ExceptWith(OriginalPerRiskSubjects);
        }

        public void Run()
        {
            BuildInitialTree();
            UpdateGraphForPerRisk();
        }

        public void BuildInitialTree()
        {
            GrowSubjectTree(OriginalSubjects, OriginalSubjects);
        }

        //this should not exceed Matrix
        public void GrowSubjectTree(HashSet<PrimarySubject> subjectList1, HashSet<PrimarySubject> subjectList2)
        {
            //we need this anyways
            foreach (PrimarySubject subject1 in subjectList1)
            {
                foreach (PrimarySubject subject2 in subjectList2)
                {
                    SubjectTreeMatrix[subject1.ID, subject2.ID] = CompareTwoSubjects(subject1, subject2);
                }
            }

            //only record the Parent for Array Form
            if (!MatrixForm)
            {
                foreach (PrimarySubject subject1 in OriginalSubjectsMinusPerRisk)
                {
                    foreach (PrimarySubject subject2 in OriginalSubjectsMinusPerRisk)
                    {
                        if (SubjectTreeMatrix[subject1.ID, subject2.ID] == SubjectCompareOutcome.Parent)
                        {
                            SubjectTreeArray[subject1.ID].Add(subject2.ID, SubjectCompareOutcome.Parent);
                        }
                    }
                }
            }
        }

        public HashSet<PrimarySubject> GetDistinctPerRiskSubjects()
        {
            HashSet<PrimarySubject> DistinctPerRiskSubjects = new HashSet<PrimarySubject>(OriginalPerRiskSubjects);
            HashSet<PrimarySubject> NewPerRiskSubjects = new HashSet<PrimarySubject>();

            //remove children Per Risk Subjects
            foreach (PrimarySubject s1 in OriginalPerRiskSubjects)
            {
                foreach (PrimarySubject s2 in OriginalPerRiskSubjects)
                {
                    if (s1.ID == s2.ID)
                        continue;
                    else if (SubjectTreeMatrix[s1.ID, s2.ID] == SubjectCompareOutcome.Equal)
                        DistinctPerRiskSubjects.Remove(s1);
                    else if (CompareTwoCOLs(s1.CauseOfLossSet.Collection, s2.CauseOfLossSet.Collection) == SubjectCompareOutcome.Equal &&
                              CompareTwoExpTypes(s1.ExposureTypes.Collection, s2.ExposureTypes.Collection) == SubjectCompareOutcome.Equal)
                    {
                        if (SubjectTreeMatrix[s1.ID, s2.ID] == SubjectCompareOutcome.Child)
                            DistinctPerRiskSubjects.Remove(s2);
                        else if (SubjectTreeMatrix[s1.ID, s2.ID] == SubjectCompareOutcome.Parent)
                            DistinctPerRiskSubjects.Remove(s1);
                        else if (SubjectTreeMatrix[s1.ID, s2.ID] == SubjectCompareOutcome.Overlap)
                        {
                            //just merge the two schedules, form a new Subject with disjoined Schedules
                            HashSet<RITE> list1 = s1.Schedule.ScheduleList;
                            HashSet<RITE> list2 = s2.Schedule.ScheduleList;

                            list1.UnionWith(list2);
                            ScheduleOfRITEs newSchedule = new ScheduleOfRITEs(s1.Schedule.Name + s2.Schedule.Name, list1, new HashSet<RITCharacteristic> { });
                            PrimarySubject newSubject = FormNewSubject(s1, newSchedule);

                            DistinctPerRiskSubjects.Remove(s1);
                            DistinctPerRiskSubjects.Remove(s2);
                            DistinctPerRiskSubjects.Add(newSubject);
                            NewPerRiskSubjects.Add(newSubject);
                            //add to SubjectTree for this new Subject
                        }
                    }
                }
            }
            foreach (PrimarySubject newS in NewPerRiskSubjects)
            {
                foreach (PrimarySubject oldS in DistinctPerRiskSubjects)
                {
                    SubjectTreeMatrix[newS.ID, oldS.ID] = CompareTwoSubjects(newS, oldS);
                }
            }
            return DistinctPerRiskSubjects;
        }

        public void UpdateGraphForPerRisk()
        {
            HashSet<PrimarySubject> DistinctPerRiskSubjects = GetDistinctPerRiskSubjects();
            Dictionary<PrimarySubject, Dictionary<string, int>> PerRiskSubjectDict = new Dictionary<PrimarySubject, Dictionary<string, int>>();

            HashSet<PrimarySubject> ExplodedSubject = new HashSet<PrimarySubject>();

            //Now we have unique list of Per Risk Subject, just explode them 
            foreach (PrimarySubject s in DistinctPerRiskSubjects)
            {
                Dictionary<string, int> temp = new Dictionary<string, int>();
                //explode them
                foreach (RITE rite in s.Schedule.ScheduleList)
                {
                    HashSet<RITE> aRite = new HashSet<RITE>();
                    aRite.Add(rite);
                    ScheduleOfRITEs newSchedule = new ScheduleOfRITEs("Exploded." + rite.ToString(), aRite, new HashSet<RITCharacteristic> { });
                    PrimarySubject newSub = FormNewSubject(s, newSchedule);
                    ExplodedSubjectList.Add(newSub);
                    temp.Add(newSchedule.Name, newSub.ID);
                }
                PerRiskSubjectDict.Add(s, temp);
            }

            foreach (PrimarySubject s in DistinctPerRiskSubjects)
            {
                foreach (PrimarySubject notPerRiskSub in OriginalSubjectsMinusPerRisk)
                {
                    //copy the result
                    if (SubjectTreeMatrix[s.ID, notPerRiskSub.ID] == SubjectCompareOutcome.Parent)
                    {
                        foreach (string rite in PerRiskSubjectDict[s].Keys)
                        {
                            SubjectTreeMatrix[PerRiskSubjectDict[s][rite], notPerRiskSub.ID] = SubjectCompareOutcome.Parent;
                        }
                    }
                    else if (SubjectTreeMatrix[s.ID, notPerRiskSub.ID] == SubjectCompareOutcome.Child || SubjectTreeMatrix[s.ID, notPerRiskSub.ID] == SubjectCompareOutcome.Overlap)
                    {
                        foreach (string rite in PerRiskSubjectDict[s].Keys)
                        {
                            if (CompareTwoSchedules(s.Schedule, notPerRiskSub.Schedule) == SubjectCompareOutcome.Parent)
                                SubjectTreeMatrix[PerRiskSubjectDict[s][rite], notPerRiskSub.ID] = SubjectCompareOutcome.Parent;
                        }
                    }
                }

                foreach (PrimarySubject other in DistinctPerRiskSubjects)
                {
                    if (s.ID != other.ID && SubjectTreeMatrix[s.ID, other.ID] == SubjectCompareOutcome.Parent)
                    {
                        foreach (string rite in PerRiskSubjectDict[s].Keys)
                        {
                            SubjectTreeMatrix[PerRiskSubjectDict[s][rite], PerRiskSubjectDict[other][rite]] = SubjectCompareOutcome.Parent;
                        }
                    }
                }
            }
        }


        public void UpdateGraphForPerRiskArrayForm()
        {
            HashSet<PrimarySubject> DistinctPerRiskSubjects = GetDistinctPerRiskSubjects();
            Dictionary<PrimarySubject, Dictionary<string, int>> PerRiskSubjectDict = new Dictionary<PrimarySubject, Dictionary<string, int>>();

            HashSet<PrimarySubject> ExplodedSubject = new HashSet<PrimarySubject>();

            //Now we have unique list of Per Risk Subject, just explode them 
            foreach (PrimarySubject s in DistinctPerRiskSubjects)
            {
                Dictionary<string, int> temp = new Dictionary<string, int>();
                //explode them
                foreach (RITE rite in s.Schedule.ScheduleList)
                {
                    HashSet<RITE> aRite = new HashSet<RITE>();
                    aRite.Add(rite);
                    ScheduleOfRITEs newSchedule = new ScheduleOfRITEs("Exploded." + rite.ToString(), aRite, new HashSet<RITCharacteristic> { });
                    PrimarySubject newSub = FormNewSubject(s, newSchedule);
                    ExplodedSubjectList.Add(newSub);
                    temp.Add(newSchedule.Name, newSub.ID);
                }
                PerRiskSubjectDict.Add(s, temp);
            }

            foreach (PrimarySubject s in DistinctPerRiskSubjects)
            {
                foreach (PrimarySubject notPerRiskSub in OriginalSubjectsMinusPerRisk)
                {
                    //copy the result
                    if (SubjectTreeMatrix[s.ID, notPerRiskSub.ID] == SubjectCompareOutcome.Parent)
                    {
                        foreach (string rite in PerRiskSubjectDict[s].Keys)
                        {
                            SubjectTreeArray[PerRiskSubjectDict[s][rite]].Add(notPerRiskSub.ID, SubjectCompareOutcome.Parent);
                        }
                    }
                    else if (SubjectTreeMatrix[s.ID, notPerRiskSub.ID] == SubjectCompareOutcome.Child || SubjectTreeMatrix[s.ID, notPerRiskSub.ID] == SubjectCompareOutcome.Overlap)
                    {
                        foreach (string rite in PerRiskSubjectDict[s].Keys)
                        {
                            if (CompareTwoSchedules(s.Schedule, notPerRiskSub.Schedule) == SubjectCompareOutcome.Parent)
                                SubjectTreeArray[PerRiskSubjectDict[s][rite]].Add(notPerRiskSub.ID, SubjectCompareOutcome.Parent);
                        }
                    }
                }

                foreach (PrimarySubject other in DistinctPerRiskSubjects)
                {
                    if (s.ID != other.ID && SubjectTreeMatrix[s.ID, other.ID] == SubjectCompareOutcome.Parent)
                    {
                        foreach (string rite in PerRiskSubjectDict[s].Keys)
                        {
                            SubjectTreeArray[PerRiskSubjectDict[s][rite]].Add(PerRiskSubjectDict[other][rite], SubjectCompareOutcome.Parent);
                        }
                    }
                }
            }
        }


        public PrimarySubject FormNewSubject(PrimarySubject s, ScheduleOfRITEs _schedule)
        {
            return new PrimarySubject(_schedule, s.ExposureTypes, s.CauseOfLossSet);
        }

        public SubjectCompareOutcome CompareTwoCOLs(HashSet<CauseOfLoss> list1, HashSet<CauseOfLoss> list2)
        {
            if (list1.Count == list2.Count && list1.IsSubsetOf(list2))
            {
                return SubjectCompareOutcome.Equal;
            }
            else if (list1.Count < list2.Count && list1.IsProperSubsetOf(list2))
            {
                return SubjectCompareOutcome.Parent;
            }
            else if (list1.Count > list2.Count && list1.IsProperSupersetOf(list2))
            {
                return SubjectCompareOutcome.Child;
            }
            else if (list1.Overlaps(list2))
            {
                return SubjectCompareOutcome.Overlap;
            }
            else
            {
                return SubjectCompareOutcome.Disjoin;
            }
        }


        public SubjectCompareOutcome CompareTwoSchedules(ScheduleOfRITEs s1, ScheduleOfRITEs s2)
        {
            HashSet<RITE> list1 = s1.ScheduleList;
            HashSet<RITE> list2 = s2.ScheduleList;

            if (list1.Count == list2.Count && list1.IsSubsetOf(list2))
            {
                return SubjectCompareOutcome.Equal;
            }
            else if (list1.Count < list2.Count && list1.IsProperSubsetOf(list2))
            {
                return SubjectCompareOutcome.Parent;
            }
            else if (list1.Count > list2.Count && list1.IsProperSupersetOf(list2))
            {
                return SubjectCompareOutcome.Child;
            }
            else if (list1.Overlaps(list2))
            {
                return SubjectCompareOutcome.Overlap;
            }
            else
            {
                return SubjectCompareOutcome.Disjoin;
            }
        }

        public SubjectCompareOutcome CompareTwoExpTypes(HashSet<ExposureType> list1, HashSet<ExposureType> list2)
        {
            if (list1.Count == list2.Count && list1.IsSubsetOf(list2))
            {
                return SubjectCompareOutcome.Equal;
            }
            else if (list1.Count < list2.Count && list1.IsProperSubsetOf(list2))
            {
                return SubjectCompareOutcome.Parent;
            }
            else if (list1.Count > list2.Count && list1.IsProperSupersetOf(list2))
            {
                return SubjectCompareOutcome.Child;
            }
            else if (list1.Overlaps(list2))
            {
                return SubjectCompareOutcome.Overlap;
            }
            else
            {
                return SubjectCompareOutcome.Disjoin;
            }
        }

        public SubjectCompareOutcome CompareTwoSubjects(PrimarySubject s1, PrimarySubject s2)
        {
            SubjectCompareOutcome colOut = CompareTwoCOLs(s1.CauseOfLossSet.Collection, s2.CauseOfLossSet.Collection);
            SubjectCompareOutcome expOut = CompareTwoExpTypes(s1.ExposureTypes.Collection, s2.ExposureTypes.Collection);
            SubjectCompareOutcome schOut = CompareTwoSchedules(s1.Schedule, s2.Schedule);

            if (colOut == SubjectCompareOutcome.Equal && expOut == SubjectCompareOutcome.Equal && schOut == SubjectCompareOutcome.Equal)
            {
                if (s1.IsPerRisk == s2.IsPerRisk)
                    return SubjectCompareOutcome.Equal;
                else if (s1.IsPerRisk && !s2.IsPerRisk)
                    return SubjectCompareOutcome.Child;
                else
                    return SubjectCompareOutcome.Parent;
            }

            else if (colOut == SubjectCompareOutcome.Disjoin || expOut == SubjectCompareOutcome.Disjoin || schOut == SubjectCompareOutcome.Disjoin)
            {
                return SubjectCompareOutcome.Disjoin;
            }

            else if ((colOut == SubjectCompareOutcome.Parent || colOut == SubjectCompareOutcome.Equal) &&
                     (expOut == SubjectCompareOutcome.Parent || expOut == SubjectCompareOutcome.Equal) &&
                     (schOut == SubjectCompareOutcome.Parent || schOut == SubjectCompareOutcome.Equal))
                return SubjectCompareOutcome.Parent;

            else if ((colOut == SubjectCompareOutcome.Child || colOut == SubjectCompareOutcome.Equal) &&
                     (expOut == SubjectCompareOutcome.Child || expOut == SubjectCompareOutcome.Equal) &&
                     (schOut == SubjectCompareOutcome.Child || schOut == SubjectCompareOutcome.Equal))
                return SubjectCompareOutcome.Child;

            else
                return SubjectCompareOutcome.Overlap;
        }
    }
    public enum SubjectCompareOutcome
    {
        Child = -1,
        Equal = 100,
        NA = 0,
        Parent = 1,
        Overlap = 2,
        Disjoin = 3
    }

}
