using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestBug
    {
        private SerializerImpl serializer;
        public enum FormatCurrency
        {
            DotSeparated,
            CommaSeparated
        }


        //private class Currency 
        //{
        //    private long totalCopecks;
        //    private const int maxCopecks = 100;

        //    public Currency()
        //    {
        //    }


        //    public Currency(long roubles, int copecks)
        //    {
        //        totalCopecks = checked(roubles * maxCopecks + copecks);
        //    }


        //    public long TotalCopecks
        //    {
        //        get { return totalCopecks; }
        //    }

        //    public long Roubles
        //    {
        //        get { return totalCopecks / maxCopecks; }
        //        private set { totalCopecks = (value * maxCopecks + Copecks); }
        //    }

        //    public int Copecks
        //    {
        //        get { return (int)(totalCopecks % maxCopecks); }
        //        private set { totalCopecks = (Roubles * maxCopecks + value); }
        //    }
        //}

        [DataContract]
        public class Currency : IEquatable<Currency>
        {
            private const int maxCopecks = 100;
            private const long maxTotalCopecks = 1000000000000000 - 1; //10^15 - 1

            private static readonly Action<Currency, long> setTotalCopecks
                = EmitSetField<Currency, long>(typeof(Currency).GetField("totalCopecks",
                                                                                      BindingFlags.NonPublic |
                                                                                      BindingFlags.Public |
                                                                                      BindingFlags.Instance));
            public static Action<TObj, TProp> EmitSetField<TObj, TProp>(FieldInfo fieldInfo)
            {
                var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                               new[] { typeof(TObj), typeof(TProp) }, typeof(TObj).Module, true);
                ILGenerator il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, fieldInfo);
                il.Emit(OpCodes.Ret);
                return (Action<TObj, TProp>)method.CreateDelegate(typeof(Action<TObj, TProp>));
            }

            private readonly long totalCopecks;

            public Currency()
            {
            }

            public Currency(long roubles, int copecks)
            {
                totalCopecks = checked(roubles * maxCopecks + copecks);
                CheckTotal(totalCopecks);
            }

            public Currency(long totalCopecks)
            {
                this.totalCopecks = totalCopecks;
                CheckTotal(totalCopecks);
            }

            //NOTE принимает рубли (дробная часть-копейки)
            public Currency(double value)
            {
                double roubles = Math.Floor(value);
                double copecks = Math.Round((value - roubles) * 100);
                totalCopecks = (long)roubles * maxCopecks + (int)copecks;
                CheckTotal(totalCopecks);
            }

            public static Currency Zero
            {
                get { return new Currency(0); }
            }

            public static Currency MaxValue
            {
                get { return new Currency(maxTotalCopecks); }
            }

            public long TotalCopecks
            {
                get { return totalCopecks; }
            }

            [DataMember]
            public long Roubles
            {
                get { return totalCopecks / maxCopecks; }
                private set { setTotalCopecks(this, value * maxCopecks + Copecks); }
            }

            [DataMember]
            public int Copecks
            {
                get { return (int)(totalCopecks % maxCopecks); }
                private set { setTotalCopecks(this, Roubles * maxCopecks + value); }
            }

            #region IEquatable<Currency> Members

            public bool Equals(Currency obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.totalCopecks == totalCopecks;
            }

            #endregion

            public bool IsZero()
            {
                return totalCopecks == 0;
            }

            [OnDeserialized]
            private void OnDeserialized(StreamingContext context)
            {
                CheckTotal(totalCopecks);
            }

            public static Currency operator +(Currency left, Currency right)
            {
                if (left == null && right == null)
                    return Zero;
                if (left == null)
                    return right;
                if (right == null)
                    return left;
                long totalCopecksSum = checked(left.TotalCopecks + right.TotalCopecks);
                return new Currency(totalCopecksSum);
            }

            public static Currency operator +(Currency left, decimal right)
            {
                if (left == null)
                    return new Currency((double)right);
                return left + new Currency((double)right);
            }

            public static Currency operator -(Currency left, Currency right)
            {
                if (left == null && right == null)
                    return Zero;
                if (left == null)
                    return new Currency(-right.totalCopecks);
                if (right == null)
                    return left;
                long totalCopecksSum = checked(left.TotalCopecks - right.TotalCopecks);
                return new Currency(totalCopecksSum);
            }

            //TODO test
            public static bool operator >(Currency left, Currency right)
            {
                if (left == null || right == null)
                    return false;
                return left.totalCopecks > right.totalCopecks;
            }

            public static bool operator >=(Currency left, Currency right)
            {
                return left > right || left == right;
            }

            public static bool operator <=(Currency left, Currency right)
            {
                return right >= left;
            }

            public static bool operator <(Currency left, Currency right)
            {
                return right > left;
            }

            private static void CheckTotal(long total)
            {
                if (Math.Abs(total) > maxTotalCopecks)
                    throw new OverflowException("big value of total copecks " + total);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof(Currency)) return false;
                return Equals((Currency)obj);
            }

            public override int GetHashCode()
            {
                return totalCopecks.GetHashCode();
            }

            public static bool operator ==(Currency left, Currency right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Currency left, Currency right)
            {
                return !Equals(left, right);
            }


            public bool IsPositive()
            {
                return totalCopecks > 0;
            }

            public bool IsNegative()
            {
                return totalCopecks < 0;
            }

            public static Currency Min(Currency left, Currency right)
            {
                return left.totalCopecks > right.totalCopecks ? right : left;
            }

            public static Currency Max(Currency left, Currency right)
            {
                return left.totalCopecks > right.totalCopecks ? left : right;
            }

            public Currency RoundToRoubles()
            {
                return new Currency(Roubles + (Copecks >= 50 ? 1 : 0), 0);
            }

            public Currency TruncateToRoubles()
            {
                return new Currency(Roubles, 0);
            }

            //TODO test
            public bool SmallerOrEqual(Currency currency)
            {
                return totalCopecks <= currency.TotalCopecks;
            }

            //TODO test
            public Currency Percent(double percent)
            {
                if (percent < 0)
                    throw new InvalidOperationException("percent " + percent + " has invalid value");
                var roubles = checked((long)(Round(totalCopecks * percent / 100)));
                return new Currency(roubles);
            }

            private static double Round(double value)
            {
                const double eps = 1e-8;

                double difference = value - Math.Floor(value);

                return (Math.Abs(0.5 - difference) > eps)
                           ? Math.Round(value)
                           : Math.Ceiling(value);
            }

            public override string ToString()
            {
                return ToString(FormatCurrency.DotSeparated);
            }

            public string ToString(FormatCurrency format)
            {
                string separator = (format == FormatCurrency.CommaSeparated) ? "," : ".";

                long absTotalCopecks = Math.Abs(totalCopecks);
                return string.Format("{0}{1}{3}{2:D2}", totalCopecks < 0 ? "-" : "", absTotalCopecks / maxCopecks,
                                     absTotalCopecks % maxCopecks, separator);
            }

            public string ToStringRounded(uint digits)
            {
                long absTotalCopecks = Math.Abs(totalCopecks);
                long rem;
                switch (digits)
                {
                    case 0:
                        rem = absTotalCopecks % maxCopecks;
                        if (rem >= 50)
                            absTotalCopecks += rem;
                        else
                            absTotalCopecks -= rem;
                        return string.Format("{0}{1}", totalCopecks < 0 ? "-" : "", absTotalCopecks / maxCopecks);
                    case 1:
                        rem = absTotalCopecks % 10;
                        if (rem >= 5)
                            absTotalCopecks += rem;
                        else
                            absTotalCopecks -= rem;
                        return string.Format("{0}{1}.{2}", totalCopecks < 0 ? "-" : "", absTotalCopecks / maxCopecks,
                                             (absTotalCopecks / 10) % 10);
                    default:
                        return ToString();
                }
            }

            //TODo тупой код, создается куча строк
            public string Format(bool onlyRoubles = false)
            {
                const int digits = 3;

                string result = "";

                string roubles = Math.Abs(Roubles).ToString();
                int iter = roubles.Length / digits;
                if (roubles.Length % digits == 0)
                    iter = iter - 1;

                result += roubles.Substring(0, roubles.Length - digits * iter);
                for (int i = iter; i > 0; i--)
                    result += "\u00A0" + roubles.Substring(roubles.Length - digits * i, digits);

                int copecksAbs = Math.Abs(Copecks);
                string copecks = (copecksAbs / 10 == 0) ? "0" + copecksAbs : copecksAbs.ToString();

                result += (Copecks == 0 && onlyRoubles) ? "" : "." + copecks;
                if (Roubles < 0 || Copecks < 0)
                    result = "-" + result;
                return result;
            }

            public static bool TryParse(string currencyString, out Currency currency)
            {
                currency = Zero;

                if (string.IsNullOrEmpty(currencyString))
                    return false;

                long roubles;
                if (!long.TryParse(ParseRoubles(currencyString), out roubles))
                    return false;

                int copecks;
                if (!int.TryParse(ParseCopecks(currencyString), out copecks))
                    return false;

                currency = new Currency(roubles, copecks);
                return true;
            }

            private static string ParseRoubles(string currencyString)
            {
                if (string.IsNullOrEmpty(currencyString)) return string.Empty;
                string[] splits = currencyString.Split(',', '.');
                if (splits.Length > 2) return string.Empty;

                long roubles;
                return long.TryParse(splits[0], out roubles) ? roubles.ToString() : string.Empty;
            }

            private static string ParseCopecks(string currencyString)
            {
                if (string.IsNullOrEmpty(currencyString)) return string.Empty;
                string[] splits = currencyString.Split(',', '.');

                switch (splits.Length)
                {
                    case 1:
                        long roubles;
                        return long.TryParse(splits[0], out roubles) ? "0" : string.Empty;
                    case 2:
                        int copecks;
                        if (splits[1].Length == 0)
                            splits[1] = "0";
                        if (splits[1].Length == 1)
                            splits[1] = splits[1] + "0";
                        if (splits[1].Length > 2)
                            splits[1] = splits[1].Substring(0, 2);
                        if (!int.TryParse(splits[1], out copecks))
                            return string.Empty;
                        if (currencyString.StartsWith("-"))
                            copecks = -copecks;
                        return copecks.ToString();
                    default:
                        return string.Empty;
                }
            }

            public double ToFloat()
            {
                return ((double)totalCopecks) / 100;
            }

            public static Currency operator /(Currency dividend, double divisor)
            {
                if (dividend == null)
                    return null;
                return new Currency(dividend.ToFloat() / divisor);
            }

            public static Currency operator *(Currency increased, double mutiplier)
            {
                if (increased == null)
                    return null;
                return new Currency(increased.ToFloat() * mutiplier);
            }
        }



        class WithCurr
        {
            public Currency Curr { get; set; }
        }
        [SetUp]
        public void SetUp()
        {
            serializer = new SerializerImpl(new Extracter());
        }

        [DataContract]
        public class Automatic<T> : IEquatable<Automatic<T>> where T : new()
        {
            public Automatic()
            {
                Value = new T();
                Auto = true;
            }

            public Automatic(T value, bool auto)
            {
                Value = value;
                Auto = auto;
            }

            [DataMember]
            public T Value { get; set; }

            [DataMember]
            public bool Auto { get; set; }

            #region IEquatable<Automatic<T>> Members

            public bool Equals(Automatic<T> obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return EqualityComparer<T>.Default.Equals(Value, obj.Value) && Auto == obj.Auto;
            }

            #endregion

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 0;
                    if (!ReferenceEquals(null, Value))
                        hashCode = Value.GetHashCode();
                    return (hashCode * 397) ^ Auto.GetHashCode();
                }
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof(Automatic<T>)) return false;
                return Equals((Automatic<T>)obj);
            }
        }


        [Test]
        public void TestCurrencyNullBug()
        {
            var z = new Automatic<Currency>() { Value = null };
            var anObject = serializer.Deserialize<WithCurr>(serializer.Serialize(z));
            Assert.IsNull(anObject.Curr);
        }
        
        [Test]
        public void TestWork()
        {
            var z = new Automatic<Currency>() { Value = new Currency(10, 1) };
            byte[] serialize = serializer.Serialize(z);
            var anObject = serializer.Deserialize<WithCurr>(serialize);
            Assert.AreEqual(10, anObject.Curr.Roubles);
            Assert.AreEqual(1, anObject.Curr.Copecks);
        }

        private class Extracter : IDataMembersExtractor
        {
            public MemberInfo[] GetMembers(Type type)
            {
                if(type == typeof(Currency))
                {
                    return new MemberInfo[]
                        {
                            typeof (Currency).GetField("totalCopecks", BindingFlags.NonPublic | BindingFlags.Instance)
                        };
                }
                PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                return propertyInfos;
            }
        }

    }
}