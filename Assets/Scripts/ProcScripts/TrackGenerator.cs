using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackGenerator : MonoBehaviour {
	KDTree activeSet;
	public GameObject user;
	public GameObject road;
	public GameObject roadBase;

	// Use this for initialization

	CubeSection[,] board;


	void Start () {
		initBoard (10, 10, 151);


		
	}
	
	// Update is called once per frame
	void Update () {

	}



	void initBoard(int H, int W, float width){
		board = new CubeSection[H, W];
		for (int i=0; i<H; i++) {
			for(int e=0;e<W;e++){
				board[i,e]=new CubeSection();
				board[i,e].width=width;
				board[i,e].leftCorner=new Vector3(i*width,0,e*width);
				board[i,e].createPoints();
				joinPointsInSection(board[i,e]);
				connectBetweenSections(i,e);
			}		
		}
	}

	void createRoad(Vector3 a, Vector3 b, bool thickRoad){
		float width = 15f;
		if (thickRoad) {
			width=1f;		
		}
		Vector3 between=b-a;
		float distance = between.magnitude; 
		GameObject roadSegment = (GameObject)Instantiate (road);
		roadSegment.transform.localScale = new Vector3(width,0.1f,distance);
		roadSegment.transform.position = a + (between *0.5f); 
		roadSegment.transform.LookAt(b);
		
		GameObject base1 = (GameObject)Instantiate (roadBase);
		base1.transform.position = a;
		base1.transform.localScale = new Vector3 (width, 0.1f, width);

		GameObject base2 = (GameObject)Instantiate (roadBase);
		base2.transform.position = b;
		base2.transform.localScale = new Vector3 (width, 0.1f, width);

		
	}

	void joinPointsInSection(CubeSection c){
		Vector3 leftCorner = c.leftCorner;
		Random.seed=(int)leftCorner.magnitude;
			for(int i=0;i<c.points.Length;i++){
				for(int e=i+1;e<c.points.Length;e++){
					int test=(int)Random.Range(0,2);
					if(test%2==0){
						c.neighbours[i].Add (c.points[e]);
						c.neighbours[e].Add (c.points[i]);
						createRoad(c.points[i],c.points[e],false);
					}
				}
			}
	}

	void connectBetweenSections(int h, int w){
		Vector3 leftCorner = board[h,w].leftCorner;
		Random.seed=(int)leftCorner.magnitude;
		for (int dh=-1; dh<=0; dh++) {
			for(int dw=-1;dw<=0;dw++){
				if(!(dh==0&&dw==0)){
					if(h+dh>-1&&w+dw>-1){
						for(int i=0;i<board[h+dh,w+dw].points.Length;i++){
							for(int e=0;e<board[h,w].points.Length;e++){
								int test=(int)Random.Range(0,3);
								if(test==0&&board[h+dh,w+dw].neighbours[i].Count<Random.Range(3,4)&&board[h,w].neighbours[e].Count<Random.Range(3,4)){
									board[h+dh,w+dw].neighbours[i].Add (board[h,w].points[e]);
									board[h,w].neighbours[e].Add (board[h+dh,w+dw].points[i]);
									createRoad(board[h+dh,w+dw].points[i],board[h,w].points[e],false);
								}
							}
						}
					}
				}
			}		
		}
	}



	
}
