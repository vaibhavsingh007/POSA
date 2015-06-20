using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheCoinJar
{
    class Program
    {
        static void Main(string[] args)
        {
            CoinJar coinjar = new CoinJar();
        }
    }

    /// <summary>
    /// Strategy: CoinJar derived class
    /// </summary>
    public class CoinJar : CoinJarBase
    {
        // Base can only be initialized from its derived class
        // Base-protected constructors.
        public CoinJar()
            : base()
        { }

        public CoinJar(int maxJarVolume)
            : base(maxJarVolume)
        { }

        // Implement the Insert coin
        public override bool InsertCoin(CoinType coin)
        {
            return base.InsertCoin(coin);
        }

        public override string ToString()
        {
            return String.Format("Current balance: {0}, Consumed volume: {1}{2}",
                Balance, CurrentVolume, IsJarAboutFull ? ", Warning! Jar Volume below 5 percent" : String.Empty);
        }

    }

    // The US Coinage enumeration
    public enum CoinType
    {
        Penny,
        Nickel,
        Dime,
        Quarter,
        HalfDollar
    }

    /// <summary>
    /// Base class for CoinJar
    /// </summary>
    public class CoinJarBase
    {

        #region Protected Fields
        protected float _consumedVolume;
        protected double _currentAmount;
        private readonly int _maxVolume;

        // Associate fixed volume for each coin based on cylinder
        // volume calculation.
        protected const float PennyVol = 0.2F;
        protected const float NickelVol = 0.5F;
        protected const float DimeVol = 0.65F;
        protected const float QuarterVol = 0.8F;
        protected const float HalfDollarVol = 0.9F; 
        #endregion

        #region ctor
        protected CoinJarBase()
            : this(32)
        { }

        protected CoinJarBase(int maxVolume)
        {
            this._maxVolume = maxVolume;
        } 
        #endregion

        /// <summary>
        /// Inserts a coin into the CoinJar
        /// </summary>
        /// <param name="coin"></param>
        /// <returns></returns>
        public virtual bool InsertCoin(CoinType coin)
        {
            // Some default implementation.
            // Generally to be overridden by the derived class

            float currentCoinVol = 0;
            double currentCoinVal = 0.0;

            switch (coin)
            {
                case CoinType.Penny:
                    {
                        currentCoinVal = 0.1;
                        currentCoinVol = PennyVol;
                    }
                    break;
                case CoinType.Nickel:
                    {
                        currentCoinVal = 0.5;
                        currentCoinVol = NickelVol;
                    }
                    break;
                case CoinType.Dime:
                    {
                        currentCoinVal = 0.10;
                        currentCoinVol = DimeVol;
                    }
                    break;
                case CoinType.Quarter:
                    {
                        currentCoinVal = 0.25;
                        currentCoinVol = QuarterVol;
                    }
                    break;
                case CoinType.HalfDollar:
                    {
                        currentCoinVal = 0.50;
                        currentCoinVol = HalfDollarVol;
                    }
                    break;
                default: break;
            }


            // Check if jar volume allows insertion
            if (_consumedVolume + currentCoinVol < _maxVolume)
            {
                // Add amount
                _currentAmount += currentCoinVal;

                // Add volume
                _consumedVolume += currentCoinVol;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Simply resets the Jar
        /// </summary>
        protected void ResetJar()
        {
            this._currentAmount = 0.0;
        }

        /// <summary>
        /// Get consumed Jar volume
        /// </summary>
        protected float CurrentVolume
        {
            get { return _consumedVolume; }
        }

        /// <summary>
        /// Get the current amount in the Jar
        /// </summary>
        protected double Balance
        {
            get { return _currentAmount; }
        }

        /// <summary>
        /// Return warning if Jar volume falls below 5%
        /// </summary>
        protected bool IsJarAboutFull
        {
            get
            {
                return (_consumedVolume < (this._consumedVolume * 0.5));
            }
        }
    }

}

