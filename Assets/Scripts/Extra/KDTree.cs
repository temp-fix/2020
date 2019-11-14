using UnityEngine;
using System.Collections;

public class KDTree {
	TreeNode root;

	public KDTree(){
		root = null;

	}

	public void add(TreeNode current, Vector3 val, int d){
		d %= 3;
		++current.activeCount;
		if (val [d] < current.val [d]) {
			if (current.leftchild == null) {
				current.leftchild = new TreeNode (val);
			} else {
				add (current.leftchild, val, d + 1);
			}		
		} else {
			if(current.rightchild==null){
				current.rightchild=new TreeNode(val);
			}else{
				add (current.rightchild,val,d+1);
			}		
		}

	}

	public void add(Vector3 val){
		if (root == null) {
			root = new TreeNode (val);
		} else {
			add (root,val,0);
		}
	}

	float nearestD;
	TreeNode nearest;

	public void findNearest(TreeNode current, Vector3 val, int d){
		if (current == null) {
			return;		
		}
		if (current.active) {
			float currentDist=Vector3.Distance(val,current.val);
			if(nearestD>currentDist){
				nearestD=currentDist;
				nearest=current;
			}		
		}
		d %= 3;
		if (current.activeCount > 0) {
			if(val[d]<current.val[d]){
				findNearest (current.leftchild,val,d+1);
				if(nearestD>current.val[d]-val[d]){
					findNearest (current.rightchild,val,d+1);
				}
			}else{
				findNearest (current.rightchild,val,d+1);
				if(nearestD>val[d]-current.val[d]){
					findNearest(current.leftchild,val,d+1);
				}
			}		
		}
	}

	public TreeNode findNearest(Vector3 val){
		nearestD = 10000000000f;
		nearest = null;
		findNearest (root, val, 0);
		return nearest;
	}

	public bool remove(TreeNode current,Vector3 val,int d){
		if (current == null) {
			return false;		
		}
		d %= 3;
		if (Vector3.Distance (val, current.val) < 0.000001f) {
			current.active=false;
			return true;
		}
		if (val [d] < current.val [d]) {
			bool removed=remove (current.leftchild, val, d + 1);
			if(removed){
				--current.activeCount;
				return true;
			}
			return false;
		} else {
			bool removed=remove (current.rightchild,val,d+1);
			if(removed){
				--current.activeCount;
				return true;
			}
			return false;
		}

	}

	public bool remove(Vector3 v){
		return remove (root,v,0);
	}



	
}
