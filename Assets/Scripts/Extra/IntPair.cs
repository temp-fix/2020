using UnityEngine;
using System.Collections;
using System;

public class IntPair :IComparable<IntPair>
{
	public int first;
	public int second;
	internal IntPair(int _first, int _second)
	{
		first = _first;
		second = _second;
	}
	
	public int CompareTo(IntPair that)
	{
		if (this.first < that.first) {
			return 1;		
		}
		if (this.first > that.first) {
			return -1;		
		}
		///////
		if (this.second < that.second) {
			return 1;		
		}
		if (this.second > that.second) {
			return -1;		
		}
		return 0;

	}
}
