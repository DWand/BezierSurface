using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LR3 {
    public class MatrixHelper {

        public static double[,] Mult(double[,] fst, double[,] snd) {
            if (fst == null || snd == null) {
                throw new NullReferenceException();
            }

            if (fst.GetLength(1) != snd.GetLength(0)) {
                throw new ArgumentException();
            }

            int m = fst.GetLength(0);
            int n = fst.GetLength(1);
            int q = snd.GetLength(1);

            double[,] res = new double[m, q];
            double sum;
            for (int i = 0; i < m; i++) {
                for (int j = 0; j < q; j++) {
                    sum = 0;
                    for (int r = 0; r < n; r++) {
                        sum += fst[i, r] * snd[r, j];
                    }
                    res[i, j] = sum;
                }
            }
            return res;
        }

    }
}
