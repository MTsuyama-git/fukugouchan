using System;

namespace Utility {
    // public class BNsslPool {
    //     BNsslPoolItem head, current, tail;
    //     int __size, __used;
    //     public static readonly int SIZE = 16;

    //     private class BNsslPoolItem {
    //         public BNssl values;
    //         public BNsslPoolItem prev, next;	    
    //         public BNsslPoolItem() {
    //             values = new BNssl[BNsslPool.SIZE];
    //         }
    //     }

    //     public BNsslPool() {
    //         head = current = tail = null;
    //         __used = 0;
    //         __size = 0;
    //     }
	
    //     public void Release(uint num) {
    //         uint offset = (__used) - 1 % SIZE;
    //         __used  -= num;
    //         while((num--) != 0) {
    //             current.values[offset].CheckTop();
    //             if(offset == 0) {
    //                 offset = SIZE - 1;
    //                 current = current.prev;
    //             }
    //             else
    //                 offset --;
    //         }
    //     }

    //     public BNssl Get(int flag)
    //         {
    //             int bnp = 0;
    //             uint loop;

    //             if(__used == __size)
    //             {
    //                 BNsslPoolItem item = new();
    //                 for(loop - 0, bnp = 0; loop++ < SIZE; ++bnp) {
    //                     item.values[bnp] = new ();
    //                 }
    //                 item.prev = tail;
    //                 item.next = null;

    //                 if(head == null) {
    //                     head = current = tail = itam;
    //                 }
    //                 else {
    //                     tail.next = item;
    //                     tail = item;
    //                     current = item;
    //                 }
    //                 __size += SIZE;
    //                 __used ++;
    //                 return item.values[0];
    //             }
    //             if(__used == 0) {
    //                 current = head;
    //             }
    //             else if(__used % SIZE == 0) {
    //                 current = current.next;
    //             }
    //             return current.vals((__used++) % SIZE);
    //         }

    //     public int Used {
    //         get { return __used; }
    //     }

    //     public int Size {
    //         get { return __size; }
    //     }
	    
    // }
    // public class BNsslStack {
    //     static readonly int START_FRAMES = 32;
	

    // }

    // public class BNsslCtx {
    //     BNsslPool pool;
    //     BNsslStack stack;
    //     uint used;
    //     bool err_stack;
    //     bool too_many;
    //     int flags;
	    
    //     public BNsslCtx() {
    //         // todo
    //         pool = new();
    //         stack = new();
    //         err_check = false;
    //         too_many = false;
    //         used =0;
    //     }

    //     public BNssl Get() {
    //         BNssl ret;
    //         if(err_check || too_many) {
    //             throw new Exception("Error of Too many");
    //         }
    //         ret = pool.Get(flags);
    //         ret.ZeroClear();
    //         used++;
    //         return ret;
    //     }
        
    //     public void end() {
    //     }
    // }

    public class BNsslPair {
        public BNssl div;
        public BNssl rem;
        public BNsslPair(in BNssl div, in BNssl rem) {
            this.div = div;
            this.rem = rem;
        }
    }
    

    public class BNssl {
        private ulong[] d;
        private int top;
        private int dmax;
        private bool neg;
        private int flags;

        private static readonly int MALLOCED = 0x01;
        private static readonly int STATIC_DATA = 0x02;
        private static readonly int CONSTTIME = 0x04;
        private static readonly int BYTES = 8;
        private static readonly int BITS2 = BYTES * 8;
        private static readonly int BITS = BITS2 * 2;
        private static readonly int BITS4 = 32;
        private static readonly int INT_MAX = 2147483647;
        private static readonly ulong MASK2  =  0xffffffffffffffffL;
        private static readonly ulong MASK2l =          0xffffffffL;
        private static readonly ulong MASK2h =  0xffffffff00000000L;
        private static readonly ulong MASK2h1 = 0xffffffff80000000L;
	
        private static ulong Lw(ulong a) {
            return a & MASK2;
        }

        private static ulong Hw(ulong a) {
            return (a >> BITS2) & MASK2;
        }


        
        public BNssl() {
            __Init();
            CheckTop();
        }

        public BNssl(in byte[] arr) {
            __Init();
            int length, offset;
            int n, i, m;
            ulong l;
            for(length = arr.Length, offset = 0; length > 0 && arr[offset] == 0; offset++, length--)
                continue;
            n = length;
            if(n == 0) {
                top = 0;
                return;
            }
            i = ((n - 1) / BYTES) + 1;
            m = ((n - 1) % BYTES);
            wExpand(in i);
            top = i;
            neg = false;
            l = 0;
            while((n--) > 0) {
                l = ((l << 8) | arr[offset++]);
                if(m-- == 0) {
                    d[--i] = l;
                    l = 0;
                    m = BYTES - 1;
                }
            }
            CorrectTop();
            return;
        }

        private void __Init() {
            flags = MALLOCED;
            top = 0;
            neg = false;
            dmax = 0;
            d = null;	    
        }


        public void CheckTop()
            {
                CorrectTop();
            }

        public void CorrectTop() {
            while(top > 0 && d[top - 1] == 0) {
                top--;
            }
            if(top == 0) {
                neg = false;
            }
        }

        public void Print() {
            int i, j, v;
            bool z = false;
            if(neg) {
                Console.Write("-");
            }
            for(i = top - 1; i >= 0; --i) {
                for(j = BITS2 - 4; j >= 0; j-=4) {
                    v = (int) ((d[i] >> j) & 0x0f);
                    if(z  || v != 0) {
                        Console.Write("{0:X}", v);
                        z = true;
                    }
                }
            }
            if(top == 0 || top == 1 && d[0] == 0)
            {
                Console.Write("0");
            }
            Console.WriteLine();
        }

        public static BNssl Add(in BNssl a, in BNssl b) {
            int cmp_res;
            bool r_neg = false;
            a.CheckTop();
            b.CheckTop();
            BNssl result;

            // 符号が等しい
            if(a.IsNeg == b.IsNeg) {
                result = Uadd(in a, in b);
                result.neg = a.IsNeg;
            }
            else {
                cmp_res = Cmp(in a, in b);
                if(cmp_res != 0) {
                    result = Usub(in a, in b);
                    result.neg = ((cmp_res > 0) ? a.IsNeg : b.IsNeg);
                }
                else {
                    // zero
                    result = new();
                    result.ZeroClear();
                }
            }
            result.CheckTop();
            return result;
        }

        public static BNssl operator +(in BNssl a, in BNssl b) => Add(in a, in b);
        public static BNssl operator -(in BNssl a, in BNssl b) => Sub(in a, in b);

        public static BNssl Sub(in BNssl a, in BNssl b) {
            int cmp_res;
            BNssl result;
            bool r_neg = false;
            a.CheckTop();
            b.CheckTop();
            if(a.neg != b.neg) {
                result = Uadd(in a, in b);
                result.neg = a.neg;
            }
            else {
                cmp_res = Ucmp(in a, in b);
                if(cmp_res != 0) {
                    result = Usub(a, b);
                    result.neg = ((cmp_res > 0)? a.IsNeg : !b.IsNeg);
                }
                else {
                    result = ValueZero;
                }
            }
            result.CheckTop();
            return result;
        }

        public static BNssl ValueOne {
            get {
                BNssl r = new();
                r.__Init();
                r.d = new ulong[]{1};
                r.top = 1;
                r.dmax = 1;
                r.neg = false;
                return r;
            }
        }

        public static BNssl ValueZero {
            get {
                BNssl r = new();
                r.__Init();
                r.ZeroClear();
                return r;
            }
        }

        public void ZeroClear() {
            neg = false;
            top = 0;
        }

        static BNssl Uadd(in BNssl a, in BNssl b) {
            int max, min, dif;
            int ap, bp, rp;
            ulong carry, t1, t2;
            BNssl r = new();

            a.CheckTop();
            b.CheckTop();
            if(a.top < b.top) {
                Uadd(b, a);
            }
            max = a.top;
            min = b.top;
            dif = max - min;

            // 桁上がりの可能性があるので+1
            r.wExpand(max + 1);
            r.top = max;
	      
            carry = AddWords(ref r.d, in a.d, in b.d, out rp, out ap, out bp, min);

            while(dif != 0) {
                dif--;
                t1 = a.d[ap++];
                t2 = (t1 + carry) & MASK2;
                r.d[rp++] = t2;
                carry &= (ulong)((t2 == 0) ? 0 : 1); 
            }
            r.d[rp] = carry;
            r.top += (int)carry;
            r.neg = false;
            r.CheckTop();
            return r;
        }

        static BNssl Usub(in BNssl a, in BNssl b) {
            int cmp_ab = Cmp(in a, in b);
            int max, min, dif;
            ulong t1, t2, borrow;
            int rp, ap, bp;
            BNssl result;

            if(cmp_ab == 0) {
                result = new ();
                result.ZeroClear();
                return result;
            }

            a.CheckTop();
            b.CheckTop();


            max = a.top;
            min = b.top;
            dif = max - min;
            if(dif < 0) {
                return Usub(in b, in a);		
            }
            result  = new();
            result.wExpand(max);
            rp = ap = bp = 0;
            // ここで最下位ワードからbの最上位ワードまで(0...b.top)までの計算を行い，その結果のcarryだけを取得する
            borrow = SubWords(ref result.d, in a.d, in b.d, ref rp, ref ap, bp, min);

            // キャリーを考慮しつつ，値をコピーしていく
            while(dif != 0) {
                dif--;
                t1 = a.d[ap++];
                t2 = (t1 - borrow) & MASK2;
                result.d[rp++] = t2;
                borrow &= (ulong)((t1 == 0) ? 1 : 0);
            }

            // 元のコードだとここで0のバイトの切り詰めを行っているが，CheckTopで十分
            result.top = max;
            result.neg = false;
            result.CheckTop();
            return result;
        }

        static ulong MulWords(ref ulong[] r, in ulong[] a, int num, ulong w)
            {
                int rp, ap;
                ulong c1 = 0;
                rp = ap = 0;
                if(num < 0)
                    throw new Exception("num should not be less than zero");
                while(num != 0) {
                    // mul();
                }
                return 0;
            }

	public static void DivWord(in ulong num, in ulong divisor, out ulong dv, out ulong rem) {
	    dv = 0;
	    rem = 0;
	}


	private static void AddWord(in ulong a, in ulong b, ref ulong carry, out ulong output) {
	    const int halfwords = sizeof(ulong) * 8 / 2 ;
	    const ulong halfwordmask = (ulong)(((ulong)1 << (halfwords)) - 1);
	    ulong al = a & halfwordmask;
	    ulong bl = b & halfwordmask;
	    ulong ah = (a >> halfwords) & halfwordmask;
	    ulong bh = (b >> halfwords) & halfwordmask;
	    
	    if(carry != 1 && carry != 0) {
		throw new Exception("carry must be 0 or 1");
	    }

	    Console.WriteLine("mask:{0:X}", halfwordmask);
	    Console.WriteLine("halfsize:{0}", halfwords);
	    ulong rl = al + bl + carry;
	    carry = (rl & ~halfwordmask) >> halfwords;
	    rl &= halfwordmask;
	    ulong rh = ah + bh + carry;
	    carry = (rh & ~halfwordmask) >> halfwords;

	    output = (rh << 32) | rl;
	    return;
	}

        static ulong AddWords(ref ulong[] r, in ulong[] a, in ulong[] b, out int rp, out int ap, out int bp, int n)
            {
                ulong ll = 0;
                ap = bp = rp = 0;
                if(n < 0){
                    throw new Exception("n must be greater than or equal to zero.");
                }
                if(n == 0){
                    return 0;
                }
                // add from LSW to MSW of b
                while(n != 0) {
		    AddWord(a[ap++], b[bp++], ref ll, out r[rp++]);
                    // ll += a[ap++] + b[bp++];
                    // r[rp++] = ll & MASK2;
                    // ll >>= BITS2;
                    n --;
                }
                return ll;
            }


        static ulong SubWords(ref ulong[] r, in ulong[] a, in ulong[] b, ref int rp, ref int ap, int bp, int n) 
            {
                ulong t1, t2;
                int c = 0;
                if(n < 0) {
                    throw new Exception("n must be greater than or equal to zero.");
                }
                if(n == 0) {
                    return 0;
                }


                // ワード(64bit)毎に見ていく．
                // あるワードにおいてa_t と b_tを比較して，
                // a_t < b_t -> キャリーあり
                // b_t > a_t -> キャリー取消
                // a_t == b_t -> キャリー継続
                while (n != 0) {
                    t1 = a[ap++];
                    t2 = b[bp++];
                    r[rp++] = (t1 - t2 - (ulong)c) & MASK2;
                    n--;
                    c = (t1 < t2) ? 1 : (t1 > t2) ? 0 : c;
                }
                return (ulong)c;
            }

        static int Ucmp(in BNssl a, in BNssl b) {
            int i, ap, bp;
            ulong t1, t2;
            a.CheckTop();
            b.CheckTop();

            i = a.top - b.top;
            if(i != 0) {
                return i;
            }
            ap = bp = 0;
            for(i = a.top - 1; i >= 0; --i)
            {
                t1 = a.d[i];
                t2 = b.d[i];
                if (t1 != t2) {
                    return (t1 > t2) ? 1 : -1;
                }
            }
            return 0;
        }

        static int Cmp(in BNssl a, in BNssl b) {
            int i, cmp_result;
            int gt, lt;
            ulong t1, t2;
            if( a == null || b == null) {
                if (a != null) return -1;
                else if(b != null) return 1;
                else return 0;
            }

            a.CheckTop();
            b.CheckTop();

            if(a.neg != b.neg) {
                if(a.neg)
                    return -1;
                else
                    return 1;
            }

            if(a.neg == false) {
                gt = 1;
                lt = -1;
            }
            else {
                gt = -1;
                lt = 1;
            }

            if (a.top > b.top)
                return gt;
            if (a.top < b.top)
                return lt;
            cmp_result = Ucmp(in a, in b);
            if(cmp_result > 0) {
                return gt;
            }
            else if(cmp_result < 0){
                return lt;
            }
            return 0;
        }

        private void Expand2(in int words) {
            if(words > dmax) {
                if(words > INT_MAX / (4 * BITS2)) {
                    throw new Exception("Bignum is too long");
                }
                if((flags & STATIC_DATA) != 0) {
                    throw new Exception("Static Data Exception");
                }
                ulong[] dest = new ulong[words];
                dmax = words;
                if(top > 0) {
                    Misc.BlockCopy(ref dest, in d, 0, top);
                }
                d = dest;
            }
        }

        private void wExpand(in int words) {
            if(words > dmax) {
                Expand2(in words);
            }
        }

        public int NumBytes {
            get {
                return (NumBits + 7) / 8;
            }
        }

        public static int NumBitsWord(ulong l) {
            ulong x, mask;
            int bits = (l != 0) ? 1 : 0;
            DumpWord(in l);
            if(BITS2 > 32) {
                x = l >> 32;
                mask = (ulong)(0 - (long)x) & MASK2;
                mask = (ulong)(0 - (mask >> (BITS2 - 1))) & MASK2;
                bits += (int)(32 & mask);
                l ^= (x ^ l) & mask;
            }
            x = l >> 16;
            mask = ((ulong)(0 - (long)x) & MASK2);
            mask = (0 - (mask >> (BITS2 - 1)));
            bits += (int)(16 & mask);
            l ^= (x ^ l) & mask;

            x = l >> 8;
            mask = (0 - x) & MASK2;
            mask = (0 - (mask >> (BITS2 - 1)));
            bits += (int)(8 & mask);
            l ^= (x ^ l) & mask;

            x = l >> 4;
            mask = (0 - x) & MASK2;
            mask = (0 - (mask >> (BITS2 - 1)));
            bits += (int)(4 & mask);
            l ^= (x ^ l) & mask;

            x = l >> 2;
            mask = (0 - x) & MASK2;
            mask = (0 - (mask >> (BITS2 - 1)));
            bits += (int)(2 & mask);
            l ^= (x ^ l) & mask;

            x = l >> 1;
            mask = (0 - x) & MASK2;
            mask = (0 - (mask >> (BITS2 - 1)));
            bits += (int)(1 & mask);
            return bits;
        }

        public static void DumpWord(in ulong v) {
            Console.WriteLine("v: 0b{0}", Convert.ToString((long)v, 2));
        }

        public int NumBits {
            get {
                int i = top - 1;
                CheckTop();
                if(IsZero) {
                    return 0;
                }
                return ((i * BITS2) + NumBitsWord(d[i]));
            }
        }

        public static BNsslPair Div(in BNssl num, in BNssl divisor)
            {
                if(divisor.IsZero) {
                    throw new Exception("Divisor is zero");
                }
                if(divisor.d[divisor.top - 1] == 0) {
                    throw new Exception("Divisor is not initialized");
                }

                return __DivFixedTop(in num, in divisor);
            }

        private static BNsslPair __DivFixedTop(in BNssl num, in BNssl divisor) {
            int norm_shift, i, j, loop;
            BNssl tmp , snum, sdiv, res;
            int wnum_ptr = 0, wnumtop_ptr = 0, resp_ptr = 0;;
            ulong d0, d1;
            int num_n, div_n;

            BNssl dv, rm;
            dv = new();
            rm = new();

            if(divisor.top <= 0 || divisor.d[divisor.top - 1] == 0) {
                throw new Exception("Divisor must be greater than 0");
            }
            num.CheckTop();
            divisor.CheckTop();
            dv.CheckTop();
            rm.CheckTop();
            res = dv;
            tmp = new();
            snum = new();
            sdiv = new();
 
            // copy divisor to sdiv
            sdiv = divisor.clone;
	    
            // sdivを左に詰める
            norm_shift = sdiv.LeftAlign();
            sdiv.neg = false;
            // snum，sdivが左につめた分だけを左につめる
            snum.LshiftFixedTop(num, norm_shift);
            div_n = sdiv.top;
            num_n = snum.top;

            // numとdivの流さを揃える
            if(num_n <= div_n) {
                snum.wExpand(div_n + 1);
                Array.Fill<ulong>(snum.d, 0, num_n, div_n - num_n + 1);
                // wExpandするとtopに補正がかかるので，元に戻す
                snum.top = num_n = div_n + 1;
            }
            loop = num_n - div_n;

            wnum_ptr = loop;
            wnumtop_ptr = num_n - 1;
	  
            d0 = sdiv.d[div_n - 1];
            d1 = (div_n == 1) ? 0 : sdiv.d[div_n - 2];
	    
            // div_nがnum_nより短い分だけ伸ばす(左に寄ってるので伸ばすだけ)
            res.wExpand(loop);
	    
            // 符号は2つの数値のxor
            res.neg = (num.neg ^ divisor.neg);
            res.top = loop;
            resp_ptr = loop;

            // 
            tmp.wExpand(div_n + 1);
            for(i = 0; i < loop; ++i, wnumtop_ptr --) {
                ulong q, l0;

                ulong n0, n1, rem = 0;
                n0 = snum.d[wnumtop_ptr];
                n1 = snum.d[wnumtop_ptr-1];
                if(n0 == d0)
                    q = MASK2;
                else {
                    ulong n2 = (ulong)((wnumtop_ptr == wnum_ptr) ? 0 : snum.d[wnumtop_ptr]);
                    // q = Math.DivRem((ulong)n0, (ulong)d0, out rem);
		    
			
                }
		
		
	
            }
            snum.neg = num.neg;
            snum.top = div_n;
            // rm.RshiftFixedTop(snum, norm_shift);
            return new BNsslPair(dv, rm);
        }


        // 最上位ワードを左につめる
        int LeftAlign()
            {
                ulong n, m, rmask;
                int top = this.top;
                int rshift = NumBitsWord(d[top - 1]), lshift, i;
                // 左側の空きを計算
                lshift = BITS2 - rshift;
                rshift %= BITS2;
                // ???
                rmask = (ulong)(0 - rshift);
                rmask |= (rmask >> 8);
                for(i = 0, m = 0; i < top; ++i){
                    n = d[i];
                    d[i] = ((n << lshift) | m) & MASK2;
                    m = (n >> rshift) & rmask;
                }
                return lshift;
            }

        void LshiftFixedTop(in BNssl a, in int n) {
            int i, nw;
            uint lb, rb;
            int tp, fp;
            ulong l, m, rmask = 0;
            if(n < 0) {
                throw new Exception("n should not be less than zero.");
            }
            this.CheckTop();
            a.CheckTop();
            // # of words
            nw = n / BITS2;
            this.wExpand(a.top + nw + 1);
            if(a.top != 0) {
                lb = (uint)( n % BITS2);
                rb = (uint)(BITS2 - lb);
                rb %= (uint)(BITS2);
                rmask = (ulong) 0 - rb;
                rmask |= rmask >> 8;
                fp = 0;
                tp = nw;
                l = a.d[fp + a.top - 1];
                this.d[tp + a.top] = ((ulong)l >> (int)rb) & rmask;
                for(i = a.top - 1; i > 0; i--) {
                    m = l << (int)lb;
                    l = a.d[fp + i - 1];
                    this.d[tp + i] = (m | (( (ulong)l >> (int)rb ) & rmask)) & MASK2;
                }
                this.d[tp] = (l << (int)lb) & MASK2;
            }
            else {
                this.d[nw] = 0;
            }
            if(nw != 0) {
                Array.Fill<ulong>(this.d, 0, 0, nw);
            }

            this.neg = a.neg;
            this.top = a.top + nw + 1;
        }

// static int bn_left_align(BIGNUM *num)
// {
//     BN_ULONG *d = num->d, n, m, rmask;
//     int top = num->top;
//     int rshift = BN_num_bits_word(d[top - 1]), lshift, i;

//     lshift = BN_BITS2 - rshift;
//     rshift %= BN_BITS2;            /* say no to undefined behaviour */
//     rmask = (BN_ULONG)0 - rshift;  /* rmask = 0 - (rshift != 0) */
//     rmask |= rmask >> 8;

//     for (i = 0, m = 0; i < top; i++) {
//         n = d[i];
//         d[i] = ((n << lshift) | m) & BN_MASK2;
//         m = (n >> rshift) & rmask;
//     }

//     return lshift;
// }

        public BNssl clone {
            get {
                BNssl result = new();
                int words;
                CheckTop();
                words = dmax;
                result.wExpand(words);
                if(top > 0)
                {
                    Misc.BlockCopy(ref result.d, this.d, 0, words);
                }
                result.neg = this.neg;
                result.top = this.top;
                result.CheckTop();

                return result;
            }
        }


        public bool AbsIsWord(in ulong w) {
            return ((top == 1) && (d[0] == w) || ((w == 0) && top == 0));
        }

        public bool IsOne {
            get {
                return (AbsIsWord(1) || IsNeg);
            }
        }

        public bool IsZero {
            get {
                return top == 0;
            }
        }

        public bool IsNeg
            {
                get
                {
                    return neg;
                }
            }

        private byte[] BinPad(in int length, in Endian endian=Endian.BIG) {
            int n;
            ulong i, lasti, j, atop, mask;
            ulong l;


            n = 0;

            return new byte[1];
        }


        public byte[] ByteArray {
            get {
                return BinPad(-1);
            }
        }
    }
}
