using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MetaboliteValidation;

namespace UnitTestMetaboliteValidation
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void DelimitedFileParser_Headers()
        {
            var aStr = "a,b,c\n1,2,3\n4,5,6";
            var a = new DelimitedFileParser();
            var b = new DelimitedFileParser();
            a.ParseString(aStr);
            b.ParseString(aStr);
            var size = a.GetHeaders().Length;
            for (var i = 0; i < size; i++)
            {
                Assert.AreEqual(b.GetHeaders()[i], a.GetHeaders()[i]);
            }
        }
        [TestMethod]
        public void DelimitedFileParser_GetAt()
        {
            var aStr = "a,b,c\n1,2,3\n4,5,6";
            var a = new DelimitedFileParser();
            a.ParseString(aStr);
            Assert.AreEqual("3", a.GetAt(0,2));
        }
        [TestMethod]
        public void DelimitedFileParser_GetAtIndexOutOfRange()
        {
            var aStr = "a,b,c\n1,2,3\n4,5,6";
            var a = new DelimitedFileParser();
            a.ParseString(aStr);
            Assert.ThrowsException<IndexOutOfRangeException>(() => a.GetAt(0, 7));
        }
        [TestMethod]
        public void DelimitedFileParser_ToString()
        {
            var aStr = "a,b,c\n1,2,3\n4,5,6";
            var a = new DelimitedFileParser();
            a.ParseString(aStr);
            Assert.AreEqual(aStr, a.ToString());
        }
        [TestMethod]
        public void DelimitedFileParser_Concat()
        {
            var aStr = "a,b,c\n1,2,3\n4,5,6";
            var bStr = "a,b,c\n7,8,9\n10,11,12";
            var fStr = "a,b,c\n1,2,3\n4,5,6\n7,8,9\n10,11,12";
            var a = new DelimitedFileParser();
            var b = new DelimitedFileParser();
            a.ParseString(aStr);
            b.ParseString(bStr);
            var success = a.Concat(b);
            Assert.IsTrue(success);
            Assert.AreEqual(fStr, a.ToString());
        }
        [TestMethod]
        public void DelimitedFileParser_ConcatFail()
        {
            var aStr = "a,b\n1,2,3\n4,5,6";
            var bStr = "a,b,c\n7,8,9\n10,11,12";
            var a = new DelimitedFileParser();
            var b = new DelimitedFileParser();
            a.ParseString(aStr);
            b.ParseString(bStr);
            var success = a.Concat(b);
            Assert.IsFalse(success);
            Assert.AreEqual(aStr, a.ToString());
        }
        [TestMethod]
        public void DelimitedFileParser_PrintAgelent()
        {
            var aStr = "###Formula\tMass\tCompound name\tKEGG\tCAS\tPolarity\tIon Species\tCCS\tZ\tGas\tCCS Standard\tNotes\n"
                          + "#Formula\tMass\tCpd\tKEGG\tCAS\tPolarity\tIon Species\tCCS\tZ\tGas\tCCS Standard\tNotes\n"
                          + "C5H9NO3\t131.0576\tTrans-4-Hydroxy-L-proline\tC01157\t51-35-4\tpositive\t(M+H)+\t130.23\t\tN2\t\t\n"
                          + "C6H10N2O4\t174.0634\tN-Alpha-Acetyl-L-Asparagine\t\t4033-40-3\tnegative\t(M-H)-\t137.87\t\tN2\t\t\n";
            var bStr = "main_class\tsubclass\tCatalog\tCompany\tkegg\tCID\tInChi\tNeutral Name\tcas\tformula\tmass\tmPlusH\tmPlusHCCS\tmPlusHRsd\tmPlusNa\tmPlusNaCCS\tmPlusNaRsd\tmMinusH\tmMinusHCCS\tmMinusHRsd\tmPlusDot\tmPlusDotCCS\tmPlusDotRSD\n"
                + "Primary Metabolite\tAmino Acid\tH54409\tSigma-Aldrich\tC01157\t5810\tPMMYEEVYMWASQN-DMTCNVIQSA-N\tTrans-4-Hydroxy-L-proline\t51-35-4\tC5H9NO3\t131.0576\t132.0655\t130.23\t0.51\t154.0473\tN/A\tN/A\t130.0496\tN/A\tN/A\t\t\t\n"
                + "Primary Metabolite\tAmino Acid\tsc-215594\tSanta Cruz Biotechnology\t\t99715\tHXFOXFJUNFFYMO-BYPYZUCNSA-N\tN-Alpha-Acetyl-L-Asparagine\t4033-40-3\tC6H10N2O4\t174.0634\t175.0713\tN/A\tN/A\t197.0532\tN/A\tN/A\t173.0555\t137.87\t0.45\t\t\t";
            var a = new DelimitedFileParser();
            a.ParseString(bStr,'\t');
            Assert.AreEqual(aStr, a.PrintAgilent());
        }
    }
}
