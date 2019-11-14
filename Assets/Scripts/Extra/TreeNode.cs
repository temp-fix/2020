using UnityEngine;
using System.Collections;

public class TreeNode {
	public TreeNode leftchild,rightchild;
	public Vector3 val;
	public int activeCount = 0;
	public bool active;
	public TreeNode(Vector3 v){
		leftchild = null;rightchild = null;
		val = v;
		active = true;
	}
	
}
