﻿using System.Collections.Generic;

namespace MRK
{
    public enum EGRPlaceType : ushort
    {
        None = 0,
        Restaurant,
        Delivery,
        Gym,
        Smoking,
        Religion,
        Cinema,
        Park,
        Mall,
        Museum,
        Library,
        Grocery,
        Apparel,
        Electronics,
        Sport,
        BeautySupply,
        Home,
        CarDealer,
        Convenience,
        Hotel,
        ATM,
        Gas,
        Hospital,
        Pharmacy,
        CarWash,
        Parking,
        CarRental,
        BeautySalons,
        EVC,

        MAX
    }

    public class EGRPlace
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string CID { get; private set; }
        public ulong CIDNum { get; private set; }
        public string Address { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public string[] Ex { get; private set; }
        public ulong Chain { get; set; } //is place related to a chain?
        public EGRPlaceType[] Types { get; private set; }

        public EGRPlace(string name, string type, ulong cid, string addr, double lat, double lng, string[] ex, ulong chain, EGRPlaceType[] types)
        {
            Name = name;
            Type = type;
            CIDNum = cid;
            CID = cid.ToString();
            Address = addr;
            Latitude = lat;
            Longitude = lng;
            Ex = ex;
            Chain = chain;
            Types = types;
        }

        public override string ToString()
        {
            return $"[{CID}] - [{Type}] - {Name} - {Address} - [{Latitude}, {Longitude}]";
        }

        public static bool operator ==(EGRPlace left, EGRPlace right)
        {
            bool lnull = ReferenceEquals(left, null);
            bool rnull = ReferenceEquals(right, null);

            if (rnull && lnull)
                return true;

            if (lnull || rnull)
                return false;

            return left.CID == right.CID;
        }

        public static bool operator !=(EGRPlace left, EGRPlace right)
        {
            bool lnull = ReferenceEquals(left, null);
            bool rnull = ReferenceEquals(right, null);

            if (rnull && lnull)
                return false;

            if (lnull || rnull)
                return true;

            return left.CID != right.CID;
        }

        public override bool Equals(object obj)
        {
            return obj is EGRPlace place &&
                   CID == place.CID;
        }

        public override int GetHashCode()
        {
            int hashCode = -770176595;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CID);
            hashCode = hashCode * -1521134295 + CIDNum.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Address);
            hashCode = hashCode * -1521134295 + Latitude.GetHashCode();
            hashCode = hashCode * -1521134295 + Longitude.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(Ex);
            hashCode = hashCode * -1521134295 + Chain.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<EGRPlaceType[]>.Default.GetHashCode(Types);
            return hashCode;
        }
    }

    public class EGRPlaceStatistics
    {
        public int Rank;
        public string Name;
        public int Likes;
    }
}
