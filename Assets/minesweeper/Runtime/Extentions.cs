using System;

public static class Extentions
{
    public static void Shuffle<T> (this Random rng, T[] array)
    {
        if (rng == null) throw new ArgumentNullException(nameof(rng));
        int n = array.Length;
        while (n > 1) 
        {
            var k = rng.Next(n--);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }
}
