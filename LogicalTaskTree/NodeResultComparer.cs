using System;
using System.Collections.Generic;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Vergleicht i.d.R. zwei oder mehr Result.ReturnObjects möglichst typgerecht über
    /// einen übergebenen Vergleichsoperator miteinander. Unterstützte Typen sind: bool, DateTime,
    /// int, double und String. Bei unbekannten Typen wird über die ToString()-Methode verglichen.
    /// </summary>
    /// <remarks>
    /// File: NodeResultComparer
    /// Autor: Erik Nagel
    ///
    /// 30.05.2013 Erik Nagel: erstellt
    /// </remarks>
    public class NodeResultComparer
    {
        #region public members

        /// <summary>
        /// Vergleicht mehrere Results über einen übergebenen Vergleichsoperator.
        /// Gibt null zurück wenn eins der beteiligten Results = null ist.
        /// </summary>
        /// <param name="comparer">Vergleichsoperator:"LT","LE","NE","EQ","GE" oder "GT"</param>
        /// <param name="results">Liste mit zu vergleichenden Objekten</param>
        /// <returns>True, False oder null</returns>
        public bool? Compare(string comparer, List<Result> results)
        {
            for (int i = 0; i < results.Count - 1; i++)
            {
                object left = null;
                object right = null;
                object leftCandidate = results[i]?.ReturnObject;
                object rightCandidate = results[i + 1]?.ReturnObject;
                if (leftCandidate == null || rightCandidate == null)
                {
                    return null;
                }
                if ((rightCandidate is int) || (leftCandidate is int))
                {
                    if (rightCandidate is int)
                    {
                        if (leftCandidate is int)
                        {
                            left = leftCandidate;
                            right = rightCandidate;
                        }
                        else
                        {
                            int tmpLeft;
                            if (Int32.TryParse(leftCandidate.ToString(), out tmpLeft))
                            {
                                left = tmpLeft;
                                right = rightCandidate;
                            }
                        }
                    }
                    else
                    {
                        int tmpRight;
                        if (Int32.TryParse(rightCandidate.ToString(), out tmpRight))
                        {
                            right = tmpRight;
                            left = leftCandidate;
                        }
                    }
                }
                if (left == null)
                {
                    if ((rightCandidate is long) || (leftCandidate is long))
                    {
                        if (rightCandidate is long)
                        {
                            if (leftCandidate is long)
                            {
                                left = leftCandidate;
                                right = rightCandidate;
                            }
                            else
                            {
                                long tmpLeft;
                                if (long.TryParse(leftCandidate.ToString(), out tmpLeft))
                                {
                                    left = tmpLeft;
                                    right = rightCandidate;
                                }
                            }
                        }
                        else
                        {
                            long tmpRight;
                            if (long.TryParse(rightCandidate.ToString(), out tmpRight))
                            {
                                right = tmpRight;
                                left = leftCandidate;
                            }
                        }
                    }
                }
                if (left == null)
                {
                    if ((rightCandidate is DateTime) || (leftCandidate is DateTime))
                    {
                        if (rightCandidate is DateTime)
                        {
                            if (leftCandidate is DateTime)
                            {
                                left = leftCandidate;
                                right = rightCandidate;
                            }
                            else
                            {
                                DateTime tmpLeft;
                                if (DateTime.TryParse(leftCandidate.ToString(), out tmpLeft))
                                {
                                    left = tmpLeft;
                                    right = rightCandidate;
                                }
                            }
                        }
                        else
                        {
                            DateTime tmpRight;
                            if (DateTime.TryParse(rightCandidate.ToString(), out tmpRight))
                            {
                                right = tmpRight;
                                left = leftCandidate;
                            }
                        }
                    }
                }
                if (left == null)
                {
                    if ((rightCandidate is double) || (leftCandidate is double))
                    {
                        if (rightCandidate is double)
                        {
                            if (leftCandidate is double)
                            {
                                left = leftCandidate;
                                right = rightCandidate;
                            }
                            else
                            {
                                double tmpLeft;
                                if (double.TryParse(leftCandidate.ToString(), out tmpLeft))
                                {
                                    left = tmpLeft;
                                    right = rightCandidate;
                                }
                            }
                        }
                        else
                        {
                            double tmpRight;
                            if (double.TryParse(rightCandidate.ToString(), out tmpRight))
                            {
                                right = tmpRight;
                                left = leftCandidate;
                            }
                        }
                    }
                }
                if (left == null)
                {
                    if ((rightCandidate is bool) || (leftCandidate is bool))
                    {
                        if (rightCandidate is bool)
                        {
                            if (leftCandidate is bool)
                            {
                                left = leftCandidate;
                                right = rightCandidate;
                            }
                            else
                            {
                                bool tmpLeft;
                                if (bool.TryParse(leftCandidate.ToString(), out tmpLeft))
                                {
                                    left = tmpLeft;
                                    right = rightCandidate;
                                }
                            }
                        }
                        else
                        {
                            bool tmpRight;
                            if (bool.TryParse(rightCandidate.ToString(), out tmpRight))
                            {
                                right = tmpRight;
                                left = leftCandidate;
                            }
                        }
                    }
                }
                if (left == null)
                {
                    left = leftCandidate.ToString();
                    right = rightCandidate.ToString();
                }
                if (!this.compare(comparer, left.GetType(), left, right))
                {
                    return false;
                }
            } // for (int i = 0; i < results.Count - 1; i++)
            return true;
        }

        #endregion public members

        #region private members

        private bool compare(string comparer, Type typeToCompare, object candidate1, object candidate2)
        {
            if (typeToCompare == typeof(bool))
            {
                switch (comparer)
                {
                    case "LT":
                        return ((((bool)candidate1) == false) && ((bool)candidate2) == true);
                    case "LE":
                        return ((((bool)candidate1) == false) || ((bool)candidate2) == true);
                    case "NE":
                        return (((bool)candidate1) != ((bool)candidate2));
                    case "EQ":
                        return (((bool)candidate1) == ((bool)candidate2));
                    case "GE":
                        return ((((bool)candidate2) == false) || ((bool)candidate1) == true);
                    case "GT":
                        return ((((bool)candidate2) == false) && ((bool)candidate1) == true);
                    default: return false;
                }
            }
            else
            {
                if (typeToCompare == typeof(DateTime))
                {
                    switch (comparer)
                    {
                        case "LT":
                            return ((DateTime)candidate1 < (DateTime)candidate2);
                        case "LE":
                            return ((DateTime)candidate1 <= (DateTime)candidate2);
                        case "NE":
                            return ((DateTime)candidate1 != (DateTime)candidate2);
                        case "EQ":
                            return ((DateTime)candidate1 == (DateTime)candidate2);
                        case "GE":
                            return ((DateTime)candidate1 >= (DateTime)candidate2);
                        case "GT":
                            return ((DateTime)candidate1 > (DateTime)candidate2);
                        default: return false;
                    }
                }
                else
                {
                    if (typeToCompare == typeof(String))
                    {
                        switch (comparer)
                        {
                            case "LT":
                                return (candidate1.ToString().CompareTo(candidate2.ToString()) < 0);
                            case "LE":
                                return (candidate1.ToString().CompareTo(candidate2.ToString()) <= 0);
                            case "NE":
                                return (candidate1.ToString().CompareTo(candidate2.ToString()) != 0);
                            case "EQ":
                                return (candidate1.ToString().CompareTo(candidate2.ToString()) == 0);
                            case "GE":
                                return (candidate1.ToString().CompareTo(candidate2.ToString()) >= 0);
                            case "GT":
                                return (candidate1.ToString().CompareTo(candidate2.ToString()) > 0);
                            default: return false;
                        }
                    }
                    else
                    {
                        if (typeToCompare == typeof(int))
                        {
                            switch (comparer)
                            {
                                case "LT":
                                    return ((int)candidate1 < (int)candidate2);
                                case "LE":
                                    return ((int)candidate1 <= (int)candidate2);
                                case "NE":
                                    return ((int)candidate1 != (int)candidate2);
                                case "EQ":
                                    return ((int)candidate1 == (int)candidate2);
                                case "GE":
                                    return ((int)candidate1 >= (int)candidate2);
                                case "GT":
                                    return ((int)candidate1 > (int)candidate2);
                                default: return false;
                            }
                        }
                        else
                        {
                            if (typeToCompare == typeof(long))
                            {
                                switch (comparer)
                                {
                                    case "LT":
                                        return ((long)candidate1 < (long)candidate2);
                                    case "LE":
                                        return ((long)candidate1 <= (long)candidate2);
                                    case "NE":
                                        return ((long)candidate1 != (long)candidate2);
                                    case "EQ":
                                        return ((long)candidate1 == (long)candidate2);
                                    case "GE":
                                        return ((long)candidate1 >= (long)candidate2);
                                    case "GT":
                                        return ((long)candidate1 > (long)candidate2);
                                    default: return false;
                                }
                            }
                            else
                            {
                                if (typeToCompare == typeof(double))
                                {
                                    switch (comparer)
                                    {
                                        case "LT":
                                            return ((double)candidate1 < (double)candidate2);
                                        case "LE":
                                            return ((double)candidate1 <= (double)candidate2);
                                        case "NE":
                                            return ((double)candidate1 != (double)candidate2);
                                        case "EQ":
                                            return ((double)candidate1 == (double)candidate2);
                                        case "GE":
                                            return ((double)candidate1 >= (double)candidate2);
                                        case "GT":
                                            return ((double)candidate1 > (double)candidate2);
                                        default: return false;
                                    }
                                }
                                else
                                {
                                    switch (comparer)
                                    {
                                        case "LT":
                                            return (candidate1.ToString().CompareTo(candidate2.ToString()) < 0);
                                        case "LE":
                                            return (candidate1.ToString().CompareTo(candidate2.ToString()) <= 0);
                                        case "NE":
                                            return (candidate1.ToString().CompareTo(candidate2.ToString()) != 0);
                                        case "EQ":
                                            return (candidate1.ToString().CompareTo(candidate2.ToString()) == 0);
                                        case "GE":
                                            return (candidate1.ToString().CompareTo(candidate2.ToString()) >= 0);
                                        case "GT":
                                            return (candidate1.ToString().CompareTo(candidate2.ToString()) > 0);
                                        default: return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion private members

    }
}
