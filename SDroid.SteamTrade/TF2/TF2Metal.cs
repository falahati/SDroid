using System;
using System.Collections.Generic;

namespace SDroid.SteamTrade.TF2
{
    /// <summary>
    ///     An immutable structure representing some amount of TF2 metal.  Values can be added, subtracted, etc.
    ///     Negative values are not allowed.  Attempting to do, for instance, TF2Metal.Zero - TF2Metal.Scrap will throw an
    ///     exception.
    /// </summary>
    public struct TF2Metal : IEquatable<TF2Metal>, IComparable<TF2Metal>, IComparable
    {
        private const int GrainPerScrap = 840;
        private const int GrainPerReclaimed = GrainPerScrap * ScrapPerReclaimed;
        private const int GrainPerRefined = GrainPerScrap * ScrapPerRefined;
        private const int ScrapPerReclaimed = 3;
        private const int ScrapPerRefined = ScrapPerReclaimed * ReclaimedPerRefined;
        private const int ReclaimedPerRefined = 3;

        /// <summary>
        ///     A grain is a made-up type of metal, allowing us to create values equal to fractions of a scrap.
        ///     Usually you will not need to work with these directly.
        ///     To get the number of Grain per Scrap, use TF2Metal.Scrap.GrainTotal
        /// </summary>
        public static readonly TF2Metal Grain = new TF2Metal(1);

        public static readonly TF2Metal Zero = new TF2Metal(0);
        public static readonly TF2Metal Scrap = Grain * GrainPerScrap;
        public static readonly TF2Metal Reclaimed = Grain * GrainPerReclaimed;
        public static readonly TF2Metal Refined = Grain * GrainPerRefined;

        /// <summary>
        ///     Creates a new TF2Metal from the given number of grains
        /// </summary>
        private TF2Metal(int numGrains)
        {
            if (numGrains < 0)
            {
                throw new ArgumentException("Cannot create a TF2Metal with negative value");
            }

            GrainTotal = numGrains;
        }

        #region Getters for scrap/refined/etc

        /// <summary>
        ///     The total overall scrap.
        ///     Example: if the value is 3 ref + 2 rec + 1 scrap, the ScrapTotal would be 3*9 + 2*3 + 1 = 34
        /// </summary>
        public double ScrapTotal
        {
            get => (double) GrainTotal / GrainPerScrap;
        }

        /// <summary>
        ///     Only the scrap portion of this TF2Metal.
        ///     Example: if the value is 3 ref + 2 rec + 1 scrap, the ScrapPart would be 1
        /// </summary>
        public int ScrapPart
        {
            get => (int) ScrapTotal % ScrapPerReclaimed;
        }

        /// <summary>
        ///     The total overall reclaimed.
        ///     Example: if the value is 3 ref + 2 rec + 1 scrap, the ReclaimedTotal would be 3*3 + 2 + 1/3 = 11.3333..
        /// </summary>
        public double ReclaimedTotal
        {
            get => (double) GrainTotal / GrainPerReclaimed;
        }

        /// <summary>
        ///     Only the reclaimed portion of this TF2Metal.
        ///     Example: if the value is 3 ref + 2 rec + 1 scrap, the ReclaimedPart would be 2
        /// </summary>
        public int ReclaimedPart
        {
            get => (int) ReclaimedTotal % ReclaimedPerRefined;
        }

        /// <summary>
        ///     The total overall refined.
        ///     Example: if the value is 3 ref + 2 rec + 1 scrap, the RefinedTotal would be 3 + 2/3 + 1/9 = 3.7777..
        /// </summary>
        public double RefinedTotal
        {
            get => (double) GrainTotal / GrainPerRefined;
        }

        /// <summary>
        ///     Only the refined portion of this TF2Metal.
        ///     Example: if the value is 3 ref + 2 rec + 1 scrap, the RefinedPart would be 3
        /// </summary>
        public int RefinedPart
        {
            get => (int) RefinedTotal;
        }

        /// <summary>
        ///     A helper-property to get the value after the decimal-point for a refined-string.
        ///     Example: the value 3 ref + 2 rec + 1 scrap is commonly written "3.77 ref", so RefinedPartDecimal = 77.
        ///     If you're just looking for the string "3.77 ref", call ToRefinedString() instead
        /// </summary>
        public int RefinedPartDecimal
        {
            get => 11 * (ScrapPart + 3 * ReclaimedPart);
        }

        /// <summary>
        ///     The total number of grains.
        ///     See the documentation for TF2Metal.Grain for an explanation of grains.
        ///     There are very few cases where you will need to use this.
        /// </summary>
        public int GrainTotal { get; }

        /// <summary>
        ///     Only the grain portion of this TF2Metal.
        ///     See the documentation for TF2Metal.Grain for an explanation of grains.
        ///     There are very few cases where you will need to use this.
        /// </summary>
        public int GrainPart
        {
            get => GrainTotal % GrainPerScrap;
        }

        /// <summary>
        ///     Returns how many of an item this TF2Metal is worth
        ///     Example: If keyPrice = 10 ref, then
        ///     (25*TF2Metal.Refined).GetPriceUsingItem(keyPrice) == 2.5
        /// </summary>
        /// <param name="itemValue">The value of the item (such as the current key-price)</param>
        public double GetPriceUsingItem(TF2Metal itemValue)
        {
            return this / itemValue;
        }

        /// <summary>
        ///     Returns how many of an item this TF2Metal is worth, plus the remainder.
        ///     Example: If keyPrice = 10 ref, then
        ///     (25*TF2Metal.Refined).GetPriceUsingItem(keyPrice, out remainder) == 2
        ///     with remainder == 5 ref
        /// </summary>
        /// <param name="itemValue">The value of the item (eg. the current key-price)</param>
        /// <param name="remainder">How much is leftover</param>
        public int GetPriceUsingItem(TF2Metal itemValue, out TF2Metal remainder)
        {
            var numItems = (int) (this / itemValue); //Calculate value first in case remainder = this
            remainder = this % itemValue;

            return numItems;
        }

        #endregion

        #region Creation methods

        /// <summary>
        ///     Creates a TF2Metal equal to the given number of ref, rounded to the nearest scrap.
        ///     The rounding is done so that, for instance, "1.11" ref is equal to 10 scrap, even
        ///     though 10 scrap is actually "1.11111..."
        /// </summary>
        public static TF2Metal FromRef(double numRef)
        {
            return Round(numRef * Refined);
        }

        /// <summary>
        ///     Creates a TF2Metal equal to the given number of ref, rounded to the nearest scrap.
        ///     The rounding is done so that, for instance, "1.11" ref is equal to 10 scrap, even
        ///     though 10 scrap is actually "1.11111..."
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if argument is null</exception>
        /// <exception cref="FormatException">Thrown if string is not a valid number</exception>
        public static TF2Metal FromRef(string numRefStr)
        {
            return FromRef(double.Parse(numRefStr));
        }

        #endregion

        #region String methods

        /// <summary>
        ///     Returns a string displaying how many of a certain item this TF2Metal represents.
        ///     Example: if the value of a TF2 key is tf2KeyValue, then
        ///     TF2Metal someValue = tf2KeyValue + TF2Metal.Refined;
        ///     someValue.ToItemString(tf2KeyValue, "key")
        ///     returns
        ///     "1 key + 1 ref"
        /// </summary>
        /// <param name="itemValue">The value of the item (eg. the current key-price)</param>
        /// <param name="itemName">The name of the item.</param>
        /// <param name="itemNamePlural">
        ///     By default, to make plural strings we attach 's' to the end.
        ///     This parameter lets you set a different plural.
        /// </param>
        /// <param name="roundDown">
        ///     If true, only the item portion of the value will be shown.
        ///     If false, the portion in ref will be shown too
        /// </param>
        // ReSharper disable once TooManyArguments
        public string ToItemString(
            TF2Metal itemValue,
            string itemName,
            string itemNamePlural = null,
            // ReSharper disable once FlagArgument
            bool roundDown = false)
        {
            if (string.IsNullOrEmpty(itemNamePlural))
            {
                itemNamePlural = itemName + "s";
            }

            if (GrainTotal == 0 || roundDown && this < itemValue)
            {
                return "0 " + itemNamePlural;
            }

            var numItems = (int) (this / itemValue);
            var leftovers = this % itemValue;

            var returnValue = "";

            if (numItems > 0)
            {
                returnValue += string.Format("{0} {1}", numItems, numItems > 1 ? itemNamePlural : itemName);
            }

            if (leftovers > Zero && !roundDown)
            {
                if (numItems > 0)
                {
                    returnValue += " + ";
                }

                returnValue += leftovers.ToRefinedString();
            }

            return returnValue;
        }

        /// <summary>
        ///     Returns a string displaying how many total refined metal this TF2Metal represents
        ///     Example: For the value 3 ref + 2 rec + 1 scrap, this method returns "3.77 ref"
        /// </summary>
        public string ToRefinedString()
        {
            return string.Format("{0}{1} ref", RefinedPart, RefinedPartDecimal > 0 ? "." + RefinedPartDecimal : "");
        }

        /// <summary>
        ///     Returns a string displaying how much metal this TF2Metal represents, broken into parts
        ///     Example: For the value 3 ref + 2 rec + 1 scrap, this method returns "3 ref + 2 rec + 1 scrap"
        /// </summary>
        /// <param name="includeScrapFractions">
        ///     If true, fractions of a scrap are included in the output (up to two decimal places)
        ///     If false, the value is rounded down to the nearest scrap.
        ///     Default is false.
        /// </param>
        public string ToPartsString(bool includeScrapFractions = false)
        {
            if (GrainTotal == 0)
            {
                return "0 ref";
            }

            var parts = new List<string>();

            if (RefinedPart > 0)
            {
                parts.Add(RefinedPart + " ref");
            }

            if (ReclaimedPart > 0)
            {
                parts.Add(ReclaimedPart + " rec");
            }

            //Scrap-case is somewhat special
            var reclaimedInScrap = (int) ReclaimedTotal * ScrapPerReclaimed;
            var scrapRemaining = ScrapTotal - reclaimedInScrap;

            if (scrapRemaining > 0)
            {
                var toStringArgument = includeScrapFractions ? "0.##" : "0";
                var numScrapString = scrapRemaining.ToString(toStringArgument);
                parts.Add(numScrapString + " scrap");
            }

            return string.Join(" + ", parts);
        }

        public override string ToString()
        {
            return ToRefinedString();
        }

        #endregion

        #region Math stuff

        /// <summary>
        ///     Returns the difference in value between two TF2Values.
        ///     This is different from operator- in that the result is never negative
        ///     (and thus never throws an exception, since TF2Values can't be negative)
        /// </summary>
        public static TF2Metal Difference(TF2Metal val1, TF2Metal val2)
        {
            return new TF2Metal(Math.Abs(val1.GrainTotal - val2.GrainTotal));
        }

        /// <summary>
        ///     Returns the maximum of two TF2Values
        ///     Example: TF2Metal.Max(TF2Metal.Scrap, TF2Metal.Refined) returns TF2Metal.Refined
        /// </summary>
        // ReSharper disable once MethodNameNotMeaningful
        public static TF2Metal Max(TF2Metal val1, TF2Metal val2)
        {
            return val1 > val2 ? val1 : val2;
        }

        /// <summary>
        ///     Returns the minimum of two TF2Values
        ///     Example: TF2Metal.Max(TF2Metal.Scrap, TF2Metal.Refined) returns TF2Metal.Scrap
        /// </summary>
        // ReSharper disable once MethodNameNotMeaningful
        public static TF2Metal Min(TF2Metal val1, TF2Metal val2)
        {
            return val1 < val2 ? val1 : val2;
        }

        /// <summary>
        ///     Round this TF2Metal up to the nearest scrap
        ///     Example: TF2Metal.Ceiling(TF2Metal.Refined / 2) = 5 scrap
        /// </summary>
        public static TF2Metal Ceiling(TF2Metal value)
        {
            return new TF2Metal((int) Math.Ceiling(value.ScrapTotal) * GrainPerScrap);
        }

        /// <summary>
        ///     Round this TF2Metal down to the nearest scrap
        ///     Example: TF2Metal.Floor(TF2Metal.Refined / 2) = 4 scrap
        /// </summary>
        public static TF2Metal Floor(TF2Metal value)
        {
            return new TF2Metal((int) Math.Floor(value.ScrapTotal) * GrainPerScrap);
        }

        /// <summary>
        ///     Round this TF2Metal to the nearest scrap.
        ///     By default, 0.5 is always rounded up.  Note that this is different from the default behavior for
        ///     Math.Round(), because its default is stupid.
        ///     Example: TF2Metal.Round(TF2Metal.Refined / 2) = 5 scrap
        /// </summary>
        public static TF2Metal Round(TF2Metal value, MidpointRounding roundingRule = MidpointRounding.AwayFromZero)
        {
            return new TF2Metal((int) Math.Round(value.ScrapTotal, roundingRule) * GrainPerScrap);
        }

        #endregion

        #region Custom operators

        public static bool operator ==(TF2Metal val1, TF2Metal val2)
        {
            return val1.GrainTotal == val2.GrainTotal;
        }

        public static bool operator !=(TF2Metal val1, TF2Metal val2)
        {
            return val1.GrainTotal != val2.GrainTotal;
        }

        public static bool operator >(TF2Metal val1, TF2Metal val2)
        {
            return val1.GrainTotal > val2.GrainTotal;
        }

        public static bool operator <(TF2Metal val1, TF2Metal val2)
        {
            return val1.GrainTotal < val2.GrainTotal;
        }

        public static bool operator >=(TF2Metal val1, TF2Metal val2)
        {
            return val1.GrainTotal >= val2.GrainTotal;
        }

        public static bool operator <=(TF2Metal val1, TF2Metal val2)
        {
            return val1.GrainTotal <= val2.GrainTotal;
        }

        public static TF2Metal operator +(TF2Metal val1, TF2Metal val2)
        {
            return new TF2Metal(val1.GrainTotal + val2.GrainTotal);
        }

        public static TF2Metal operator -(TF2Metal val1, TF2Metal val2)
        {
            return new TF2Metal(val1.GrainTotal - val2.GrainTotal);
        }

        public static TF2Metal operator *(TF2Metal val1, double val2)
        {
            return new TF2Metal((int) (val1.GrainTotal * val2));
        }

        public static TF2Metal operator *(double val1, TF2Metal val2)
        {
            return new TF2Metal((int) (val1 * val2.GrainTotal));
        }

        public static double operator /(TF2Metal val1, TF2Metal val2)
        {
            return (double) val1.GrainTotal / val2.GrainTotal;
        }

        public static TF2Metal operator /(TF2Metal val1, double val2)
        {
            return new TF2Metal((int) (val1.GrainTotal / val2));
        }

        public static TF2Metal operator %(TF2Metal val1, TF2Metal val2)
        {
            return new TF2Metal(val1.GrainTotal % val2.GrainTotal);
        }

        #endregion

        #region IEquatable/IComparable

        public bool Equals(TF2Metal other)
        {
            return GrainTotal == other.GrainTotal;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TF2Metal))
            {
                return false;
            }

            return Equals((TF2Metal) obj);
        }

        public override int GetHashCode()
        {
            return GrainTotal.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            if (!(obj is TF2Metal))
            {
                return 1;
            }

            return CompareTo((TF2Metal) obj);
        }

        public int CompareTo(TF2Metal other)
        {
            return GrainTotal.CompareTo(other.GrainTotal);
        }

        #endregion
    }
}