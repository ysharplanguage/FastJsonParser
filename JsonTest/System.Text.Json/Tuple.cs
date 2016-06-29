namespace System {
    public class Tuple {
        public static Tuple<T1> Create<T1>(T1 t1) {
            return new Tuple<T1>(t1);
        }

        public static Tuple<T1, T2> Create<T1, T2>(T1 t1, T2 t2) {
            return new Tuple<T1, T2>(t1, t2);
        }

        public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 t1, T2 t2, T3 t3) {
            return new Tuple<T1, T2, T3>(t1, t2, t3);
        }

        static Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4) {
            return new Tuple<T1, T2, T3, T4>(t1, t2, t3, t4);
        }

        static Tuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) {
            return new Tuple<T1, T2, T3, T4, T5>(t1, t2, t3, t4, t5);
        }

        static Tuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) {
            return new Tuple<T1, T2, T3, T4, T5, T6>(t1, t2, t3, t4, t5, t6);
        }

        static Tuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7) {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7>(t1, t2, t3, t4, t5, t6, t7);
        }

        static Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> Create<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, TRest Rest) {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(t1, t2, t3, t4, t5, t6, t7, Rest);
        }
    }

    public class Tuple<T1> : Tuple {
        public T1 Item1 { get; set; }

        public Tuple(T1 Item1) {
            this.Item1 = Item1;
        }
    }

    public class Tuple<T1, T2> {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }

        public Tuple(T1 Item1, T2 Item2) {
            this.Item1 = Item1;
            this.Item2 = Item2;
        }
    }

    public class Tuple<T1, T2, T3> {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }

        public Tuple(T1 Item1, T2 Item2, T3 Item3) {
            this.Item1 = Item1;
            this.Item2 = Item2;
            this.Item3 = Item3;
        }
    }

    public class Tuple<T1, T2, T3, T4> {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }

        public Tuple(T1 Item1, T2 Item2, T3 Item3, T4 Item4) {
            this.Item1 = Item1;
            this.Item2 = Item2;
            this.Item3 = Item3;
            this.Item4 = Item4;
        }
    }

    public class Tuple<T1, T2, T3, T4, T5> {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }
        public T5 Item5 { get; set; }

        public Tuple(T1 Item1, T2 Item2, T3 Item3, T4 Item4, T5 Item5) {
            this.Item1 = Item1;
            this.Item2 = Item2;
            this.Item3 = Item3;
            this.Item4 = Item4;
            this.Item5 = Item5;
        }
    }

    public class Tuple<T1, T2, T3, T4, T5, T6> {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }
        public T5 Item5 { get; set; }
        public T6 Item6 { get; set; }

        public Tuple(T1 Item1, T2 Item2, T3 Item3, T4 Item4, T5 Item5, T6 Item6) {
            this.Item1 = Item1;
            this.Item2 = Item2;
            this.Item3 = Item3;
            this.Item4 = Item4;
            this.Item5 = Item5;
            this.Item6 = Item6;
        }
    }

    public class Tuple<T1, T2, T3, T4, T5, T6, T7> {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }
        public T5 Item5 { get; set; }
        public T6 Item6 { get; set; }
        public T7 Item7 { get; set; }

        public Tuple(T1 Item1, T2 Item2, T3 Item3, T4 Item4, T5 Item5, T6 Item6, T7 Item7) {
            this.Item1 = Item1;
            this.Item2 = Item2;
            this.Item3 = Item3;
            this.Item4 = Item4;
            this.Item5 = Item5;
            this.Item6 = Item6;
            this.Item7 = Item7;
        }
    }

    public class Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }
        public T5 Item5 { get; set; }
        public T6 Item6 { get; set; }
        public T7 Item7 { get; set; }
        public TRest Rest { get; set; }

        public Tuple(T1 Item1, T2 Item2, T3 Item3, T4 Item4, T5 Item5, T6 Item6, T7 Item7, TRest Rest) {
            this.Item1 = Item1;
            this.Item2 = Item2;
            this.Item3 = Item3;
            this.Item4 = Item4;
            this.Item5 = Item5;
            this.Item6 = Item6;
            this.Item7 = Item7;
            this.Rest = Rest;
        }
    }
}
