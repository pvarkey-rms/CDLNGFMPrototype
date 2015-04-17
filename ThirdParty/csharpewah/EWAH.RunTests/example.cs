using System;
using Ewah;

/*
 * Copyright 2012, Kemal Erdogan, Daniel Lemire and Ciaran Jessup
 * Licensed under APL 2.0.
 */

public static class example
{
    public static void Main(string[] args)
    {

        //RunAllTests();

        var ewahBitmap1 = EwahCompressedBitArray.BitmapOf(345, 100987, 4309222);
        var ewahBitmap1_clone = EwahCompressedBitArray.BitmapOf(345, 100987, 4309222);
        var ewahBitmap1_strictSubset = EwahCompressedBitArray.BitmapOf(345, 100987);
        var ewahBitmap1_notstrictSubset2 = EwahCompressedBitArray.BitmapOf(100987, 7007);
        var ewahBitmap1_intersects_1 = EwahCompressedBitArray.BitmapOf(345, 67773, 100987);
        var ewahBitmap1_intersects_2 = EwahCompressedBitArray.BitmapOf(65, 345);
        var ewahBitmap1_3 = EwahCompressedBitArray.BitmapOf(55, 900000008);

        Console.WriteLine(ewahBitmap1_clone.IsSubsetOf(ewahBitmap1));
        Console.WriteLine(ewahBitmap1.IsSubsetOf(ewahBitmap1_clone));
        Console.WriteLine(ewahBitmap1.Equals(ewahBitmap1_clone));
        Console.WriteLine(ewahBitmap1_strictSubset.IsSubsetOf(ewahBitmap1));
        Console.WriteLine(ewahBitmap1_notstrictSubset2.IsSubsetOf(ewahBitmap1));
        Console.WriteLine(ewahBitmap1_intersects_1.IsSubsetOf(ewahBitmap1));
        Console.WriteLine(ewahBitmap1_intersects_2.IsSubsetOf(ewahBitmap1));

        Console.WriteLine(ewahBitmap1.Minus(ewahBitmap1_strictSubset));
        Console.WriteLine(ewahBitmap1.Minus(ewahBitmap1_intersects_1));
        Console.WriteLine(ewahBitmap1.Minus(ewahBitmap1_intersects_2));
        Console.WriteLine(ewahBitmap1.Minus(ewahBitmap1_3));
    }

    private static bool IsSubsetOf(this EwahCompressedBitArray PurportedSubset, EwahCompressedBitArray Set)
    {
        return Set.Or(PurportedSubset).Equals(Set);
    }

    private static bool Overlaps(this EwahCompressedBitArray PurportedOverlappingSet, EwahCompressedBitArray Set)
    {
        return Set.And(PurportedOverlappingSet).GetCardinality() > 1;
    }

    private static EwahCompressedBitArray Minus(this EwahCompressedBitArray FromThis, EwahCompressedBitArray SubtractThis)
    {
        return FromThis.AndNot(SubtractThis);
    }

    public static void RunAllTests()
    {
        var ewahBitmap1 = EwahCompressedBitArray.BitmapOf(0,2,64,1 << 30);
        var ewahBitmap2 = EwahCompressedBitArray.BitmapOf(1,3,64,1 << 30);
        Console.WriteLine("Running demo program:");
        Console.WriteLine("bitmap 1: "+ewahBitmap1 );
        Console.WriteLine("bitmap 2:"+ewahBitmap2);
        EwahCompressedBitArray orbitmap = ewahBitmap1.Or(ewahBitmap2);
        Console.WriteLine();
        Console.WriteLine("bitmap 1 OR bitmap 2:"+orbitmap);
        Console.WriteLine("memory usage: " + orbitmap.SizeInBytes + " bytes");
        Console.WriteLine();
        EwahCompressedBitArray andbitmap = ewahBitmap1.And(ewahBitmap2);
        Console.WriteLine("bitmap 1 AND bitmap 2:"+andbitmap);
        Console.WriteLine("memory usage: " + andbitmap.SizeInBytes + " bytes");
        EwahCompressedBitArray xorbitmap = ewahBitmap1.Xor(ewahBitmap2);
        Console.WriteLine("bitmap 1 XOR bitmap 2:"+xorbitmap);
        Console.WriteLine("memory usage: " + andbitmap.SizeInBytes + " bytes");
        Console.WriteLine("End of demo.");
        Console.WriteLine("");
        var tr = new EwahCompressedBitArrayTest();
        tr.TestYnosa();
        tr.TestIntersectOddNess();
        tr.testsetSizeInBits();
        tr.SsiYanKaiTest();
        tr.testDebugSetSizeInBitsTest();
        tr.EwahIteratorProblem();
        tr.TayaraTest();
        tr.TestNot();
        tr.TestCardinality();
        tr.TestEwahCompressedBitArray();
        tr.TestExternalization();
        tr.TestLargeEwahCompressedBitArray();
        tr.TestMassiveAnd();
        tr.TestMassiveAndNot();
        tr.TestMassiveOr();
        tr.TestMassiveXOR();
        tr.HabermaasTest();
        tr.VanSchaikTest();
        tr.TestRunningLengthWord();
        tr.TestSizeInBits1();
        tr.TestHasNextSafe();
        tr.TestCloneEwahCompressedBitArray();
        tr.TestSetGet();
        tr.TestWithParameters();

        new EWAHCompressedBitArraySerializerTest().TestCustomSerializationStrategy();
    }
}