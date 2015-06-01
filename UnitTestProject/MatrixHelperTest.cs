using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LR3;

namespace UnitTestProject {
    [TestClass]
    public class MatrixHelperTest {
        [TestMethod]
        public void TestMultiplication() {
            double[,] m1 = new double[,] {
                {-1d, 5d},
                { 1d, 3d}
            };
            double[,] m2 = new double[,] {
                { 6d,  0d, -3d},
                {-4d, -2d,  5d}
            };
            double[,] desiredRes = new double[,] {
                {-26d, -10d, 28d},
                { -6d, -6d, 12d}
            };
            double[,] realRes = MatrixHelper.Mult(m1, m2);
            for (int i = 0; i < desiredRes.GetLength(0); i++) {
                for (int j = 0; j < desiredRes.GetLength(1); j++) {
                    Assert.AreEqual(desiredRes[i,j], realRes[i,j]);
                }
            }
            
        }
    }
}
