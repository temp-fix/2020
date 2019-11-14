using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackGeneratorVersion2 : MonoBehaviour {
	bool[,] gridForRoadPlacement;
	int[,] populationDensity,numberOfIntersections,currentPathLength;
	Vector2[,] roadDirection;
	bool[,] walkable;
	KDTree roadPointsPlaced;//

	public GameObject roadpiece;
	public float trackScaleFactor=16;
	//for debug
	public GameObject blockView;
	private List<GameObject> visibleBlocks=new List<GameObject>();

	void showCurrentTrack(){
		foreach(GameObject toRemove in visibleBlocks){
			GameObject.Destroy(toRemove);
		}
		visibleBlocks.Clear ();
		for(int y=0;y<worldHeight;y++){
			for(int x=0;x<worldWidth;x++){
				Vector3 currentPosition=new Vector3(x,0,y);
				GameObject currentBlock=(GameObject)Instantiate(blockView,currentPosition,Quaternion.identity);
				if(gridForRoadPlacement[y,x]){
					currentBlock.GetComponent<Renderer>().material.color=Color.red;
				}else if(!walkable[y,x]){
					currentBlock.GetComponent<Renderer>().material.color=Color.black;
				}

				visibleBlocks.Add(currentBlock);
			}
		}

	}

	//

	int worldHeight,worldWidth;


	// Use this for initialization
	void Start () {
		Random.seed = 0;
		initGridsForGeneration (200, 200, 0, 10, 0.7, 5);
		createRoadNetwork ();
		//generateRoadNetwork ();
		//generateWalls ();
		showCurrentTrack ();
		

		//Debug.Log (getSpaceAroundPoint (0, 0, 10));

	}

	void generateRoadNetwork(){
		bool[,] seen = new bool[worldHeight, worldWidth];
		for(int i=0;i<worldHeight;i++){
			for(int e=0;e<worldWidth;e++){
				seen[i,e]=false;
			}
		}
		Queue<Vector4> activePoints = new Queue<Vector4> ();
		activePoints.Enqueue (new Vector4 (worldHeight / 2, worldWidth / 2,-1,-1));
		while (activePoints.Count>0) {
			Vector4 currentState=activePoints.Dequeue();
			int currenty=(int)currentState.x,currentx=(int)currentState.y,prevy=(int)currentState.z,prevx=(int)currentState.w;
			if(!seen[currenty,currentx]){
				seen[currenty,currentx]=true;
				Vector4 nextPoint=new Vector4(-1,0,0,0);
				for(int dy=-1;dy<=1;dy++){
					for(int dx=-1;dx<=1;dx++){
						if(inBoundsOfWorld(currenty+dy,currentx+dx)){
							if(gridForRoadPlacement[currenty+dy,currentx+dx]){
								activePoints.Enqueue(new Vector4(currenty+dy,currentx+dx,currenty,currentx));
								//nextPoint=new Vector4(currenty+dy,currentx+dx,currenty,currentx);
							}

						}
					}
				}
				/*if(nextPoint.x!=-1){
					activePoints.Enqueue(nextPoint);
				}*/
			}
			if(prevy!=-1){

				createRoadBetweenPoints(new Vector3(prevy,0,prevx),new Vector3(currenty,0,currentx));
			}
		}

	}
	
	// Update is called once per frame
	void Update () {

	}

	void generateWalls(){
		for(int y=0;y<worldHeight;y++){
			for(int x=0;x<worldWidth;x++){
				if(gridForRoadPlacement[y,x]){
					createWallBetweenPoints(new Vector3(y,0,x),new Vector3(y+1,0,x+1));
				}
			}
		}
	}

	Vector2 getRandomDirection(Vector2 disallowedDirection){
		Vector2 randomVect = new Vector2 (0, 0);
		while (randomVect.magnitude<0.000001&&Vector2.Distance(disallowedDirection,randomVect)<0.000001) {
			randomVect.x=Random.Range(-1,2);
			randomVect.y=Random.Range(-1,2);

		}
		return randomVect;
	}

	bool surroundingsDoNotHaveRoads(int y, int x){
		for (int i=-1; i<=1; i++) {
			for(int e=-1;e<=1;e++){
				if(inBoundsOfWorld(y+i,x+e)){
					if(gridForRoadPlacement[y+i,x+e]){
						return false;
					}
				}
			}		
		
		}
		return true;
	}

	void createRoadNetwork(){
		Queue<Vector3> activePoints = new Queue<Vector3> ();//y,x,dist since last branched

		int starty = worldHeight/2, startx = worldWidth/2;
		activePoints.Enqueue (new Vector4 (starty, startx, 0, 0));
		roadDirection [starty, startx] = new Vector2 (1, 0);
		int iterations = 0;
		int allowedIterations = 1000;
		int minDistExtension = 2, maxDistExtension = 20;
		int minDistBeforeBranch = 30, maxDistBeforeBranch = 60;
		double probabilityChangeDirection = 0.1;
		/*int iterations = 0;
		int allowedIterations = 1000;
		int minDistExtension = 1, maxDistExtension = 2;
		int minDistBeforeBranch = 100, maxDistBeforeBranch = 200;
		double probabilityChangeDirection = 0.1;*/
		while (iterations<allowedIterations&&activePoints.Count>0) {
			//Debug.Log (iterations);
			++iterations;
			Vector3 currentPoint=activePoints.Dequeue();
			int currenty=(int)currentPoint.x;
			int currentx=(int)currentPoint.y;
			int currentDistSinceLastBranched=(int)currentPoint.z;
			bool branch=false;
			if(Random.Range(minDistBeforeBranch,maxDistBeforeBranch)<=currentDistSinceLastBranched){
				branch=true;
			}else{
				//Debug.Log (currentDistSinceLastBranched);
			}
			if(branch){
				currentDistSinceLastBranched=0;
			}
			int currentExtendedDistance=Random.Range(minDistExtension,maxDistExtension);
			int dy=(int)roadDirection[currenty,currentx].x;
			int dx=(int)roadDirection[currenty,currentx].y;
			int newy=dy*currentExtendedDistance+currenty,newx=dx*currentExtendedDistance+currentx;

			int endx=newx+dx,endy=newy+dy;
			Vector2 actualNewPoint=assignRoadBetweenPoints(currenty,currentx,endy,endx,dy,dx);
			int actualy=(int)actualNewPoint.x,actualx=(int)actualNewPoint.y;
			if(actualy==endy){//road addition was successful
				if(inBoundsOfWorld(newy,newx)){
					double testStatistic=Random.Range(0f,1f);
					Vector2 newDirection=roadDirection[newy,newx];
					if(testStatistic<probabilityChangeDirection){
						newDirection=getRandomDirection(new Vector2(-dy,-dx));
					}
					roadDirection[newy,newx]=newDirection;
					activePoints.Enqueue(new Vector3(newy,newx,currentDistSinceLastBranched+currentExtendedDistance));

				}
				//actualy-=dy;actualx-=dx;
				//check if should change direction


			}else{
				//Debug.Log ("!!");
			}

			if(branch){
				//continue;
				Vector2 newDirection=getRandomDirection(new Vector2(0,0));
				//Vector2 currentRoadDirection=roadDirection[currenty,currentx];

				int branchdy=(int)newDirection.x,branchdx=(int)newDirection.y;
				int branchy=currenty+branchdy,branchx=currentx+branchdx;
				if(inBoundsOfWorld(branchy,branchx)){
					roadDirection[branchy,branchx]=newDirection;
					gridForRoadPlacement[branchy,branchx]=true;
					activePoints.Enqueue(new Vector3(branchy,branchx,0));
				}

			}



		}

	}

	Vector2 assignRoadBetweenPoints(int beginy, int beginx, int endy, int endx,int dy, int dx){//returns endpoint of road
		int y = beginy+dy, x = beginx+dx;
		while (inBoundsOfWorld(y,x)&&(!(y==endy&&x==endx))) {
			if(gridForRoadPlacement[y,x]==true){
				break;
			}
			gridForRoadPlacement[y,x]=true;
			roadDirection[y,x]=new Vector2(dy,dx);
			y+=dy;x+=dx;

		}

		createRoadBetweenPoints (new Vector3 (beginy, 0, beginx), new Vector3 (y, 0, x));


		return new Vector2 (y, x);
	}

	void createRoadBetweenPoints(Vector3 beginPoint, Vector3 endPoint){

		float width = 15f;
		beginPoint = trackScaleFactor * beginPoint;
		endPoint = trackScaleFactor * endPoint;
		Vector3 between=endPoint-beginPoint;
		float distance = between.magnitude+0.5f*trackScaleFactor; 
		//Debug.Log (distance);
		GameObject roadSegment = (GameObject)Instantiate (roadpiece);
		roadSegment.transform.localScale = new Vector3(width,0.1f,distance);
		roadSegment.transform.position = beginPoint+ (between *0.5f); 
		roadSegment.transform.LookAt(endPoint);
		
		/*GameObject base1 = (GameObject)Instantiate (roadBase);
		base1.transform.position = a;
		base1.transform.localScale = new Vector3 (width, 0.1f, width);
		
		GameObject base2 = (GameObject)Instantiate (roadBase);
		base2.transform.position = b;
		base2.transform.localScale = new Vector3 (width, 0.1f, width);*/
		
		
	}
	void createWallBetweenPoints(Vector3 beginPoint, Vector3 endPoint){

		beginPoint = trackScaleFactor * beginPoint;
		endPoint = trackScaleFactor * endPoint;
		Vector3 between=endPoint-beginPoint;
		float distance = between.magnitude; 
		GameObject roadSegment = (GameObject)Instantiate (roadpiece);
		roadSegment.transform.localScale = new Vector3(trackScaleFactor,trackScaleFactor,distance);
		roadSegment.transform.position = beginPoint+ (between *0.5f); 
		roadSegment.transform.LookAt(endPoint);
		
		/*GameObject base1 = (GameObject)Instantiate (roadBase);
		base1.transform.position = a;
		base1.transform.localScale = new Vector3 (width, 0.1f, width);
		
		GameObject base2 = (GameObject)Instantiate (roadBase);
		base2.transform.position = b;
		base2.transform.localScale = new Vector3 (width, 0.1f, width);*/
		
		
	}

				 




	int getSpaceAroundPoint(int y, int x, int maxDistance, bool[,] roadsAlreadyPlaced){
		//Debug.Log ("!!!");
		Queue<Vector3> activePoints = new Queue<Vector3> ();
		bool[,] seen=new bool[maxDistance*2+2,maxDistance*2+2];
		for(int i=0;i<maxDistance*2+1;i++){
			for(int e=0;e<maxDistance*2+1;e++){
				seen[i,e]=false;
			}
		}
		seen [maxDistance, maxDistance] = true;
		activePoints.Enqueue (new Vector3 (y, x, 0));

		int space = 0;
		while (activePoints.Count>0) {
			Vector3 currentPoint=activePoints.Dequeue();
			int currenty=(int)currentPoint.x;
			int currentx=(int)currentPoint.y;
			if(currentPoint.z>=maxDistance){
				break;
			}
			for(int dy=-1;dy<=1;dy++){
				for(int dx=-1;dx<=1;dx++){
					if(dx!=0&&dy!=0){
						break;
					}
					if(inBoundsOfWorld(currenty+dy,currentx+dx)){
						int newy=currenty+dy,newx=currentx+dx;
						//Debug.Log ((newy-y+maxDistance)+" "+(newx-x+maxDistance));
						if((!seen[newy-y+maxDistance,newx-x+maxDistance])&&(walkable[newy,newx])&&(!roadsAlreadyPlaced[newy,newx])){
							seen[newy-y+maxDistance,newx-x+maxDistance]=true;
							++space;
							activePoints.Enqueue(new Vector3(newy,newx,currentPoint.z+1));
						}

					}
				}
			}
		}
		return space;
	}




	void automateRoads(){
		bool[,] newRoadPlacement=new bool[worldHeight,worldWidth];
		for (int y=0; y<worldHeight; y++) {
			for (int x=0; x<worldWidth; x++) {
				newRoadPlacement[y,x]=gridForRoadPlacement[y,x];
			}
		}

		for(int y=0;y<worldHeight;y++){
			for(int x=0;x<worldWidth;x++){
				if(gridForRoadPlacement[y,x]){
					int bestx=0, besty=0, bestSpace=-1;
					for(int dy=-1;dy<=1;dy++){
						for(int dx=-1;dx<=1;dx++){
							int newy=y+dy,newx=x+dx;
							if(inBoundsOfWorld(newy,newx)){
								if(!gridForRoadPlacement[newy,newx]&&!newRoadPlacement[newy,newx]){
									int currentSpace=getSpaceAroundPoint(newy,newx,5,newRoadPlacement);
									if(currentSpace>bestSpace){
										bestSpace=currentSpace;
										bestx=newx;
										besty=newy;
									}
								}
								
							}
						}
					}
					if(bestSpace>5){
						Debug.Log (bestSpace);
						newRoadPlacement[besty,bestx]=true;
					}
				}
			}
		}
		gridForRoadPlacement = newRoadPlacement;
	

	
	}







	void generateTrack(){
				//
				//randomly place non-walkable zones and spread
				//randomly init population density (every interval, then interpolate)
				//randomly start highway, check 4 surrounding directions and move to highest density such that location is acceptable (density tollerance), add all seen points to kd tree
				//randomly place road
				//randomly select preexisting road piece and attempt to extend (length of road selected, check if acceptable), then extend
				




	}





	bool inBoundsOfWorld(int y, int x){
		if (0 <= y && y < worldHeight) {
			if(0<=x && x<worldWidth){
				return true;
			}		
		}
		return false;

	}

	bool inBoundsOfWorld(Vector2 position){
		int y = (int)position.x;
		int x = (int)position.y;
		return inBoundsOfWorld (y, x);
		
	}
	void initGridsForGeneration(int heightOfWorld, int widthOfWorld, int numberOfNonWalkableLocations, int maxNonWalkableSize, double probabilityToExpandNonWalkableRegion, int populationGenerationBlockSize){
		worldHeight = heightOfWorld;
		worldWidth = widthOfWorld;
		gridForRoadPlacement=new bool[heightOfWorld,widthOfWorld];
		walkable=new bool[heightOfWorld,widthOfWorld];
		populationDensity=new int[heightOfWorld,widthOfWorld];
		numberOfIntersections=new int[heightOfWorld,widthOfWorld];
		currentPathLength = new int[heightOfWorld, widthOfWorld];
		roadDirection = new Vector2[heightOfWorld, widthOfWorld];
		roadPointsPlaced=new KDTree();
		////init gridForRoadPlacement, walkable, numberOfIntersections
		for (int y=0; y<worldHeight; y++) {
			for(int x=0;x<worldWidth;x++){
				gridForRoadPlacement[y,x]=false;
				walkable[y,x]=true;
				numberOfIntersections[y,x]=0;
				populationDensity[y,x]=-1;
				currentPathLength[y,x]=0;
				roadDirection[y,x]=new Vector2(0,0);
			}		
		}






		//init non-walkable regions
		for(int i=0;i<numberOfNonWalkableLocations;i++){
			int sizeOfRegion=0;
			int maxIterations=sizeOfRegion*4;
			Queue<Vector2> activePoints=new Queue<Vector2>();
			Vector2 start=new Vector2(Random.Range(0,heightOfWorld-1),Random.Range(0,widthOfWorld-1));
			activePoints.Enqueue(start);
			while((activePoints.Count>0)&&sizeOfRegion<maxNonWalkableSize){
				Vector2 currentPoint=activePoints.Dequeue();
				int currentX=(int)currentPoint.y;
				int currentY=(int)currentPoint.x;
				for(int dx=-1;dx<=1;dx++){
					for(int dy=-1;dy<=1;dy++){
						if(inBoundsOfWorld((int)currentY+dy,(int)currentX+dx)){
							if(walkable[currentY+dy,currentX+dx]){
								double testStatistic=Random.Range(0f,1f);
								if(probabilityToExpandNonWalkableRegion>testStatistic){
									//extend
									walkable[currentY+dy,currentX+dx]=false;
									++sizeOfRegion;
									activePoints.Enqueue(new Vector2(currentY+dy,currentX+dx));
								}
							}
						}
					}
				}
			}
		}
		////////end init non-walkabke regions

		////init populationDensity using (Perlin noise?)

		Queue<Vector2> activeBlocks = new Queue<Vector2> ();
		Vector2 startBlock = new Vector2 (0, 0);
		while (activeBlocks.Count>0) {
			Vector2 currentBlock=activeBlocks.Dequeue();
			//extend corners
			Vector2 diagonalCorner=new Vector2(currentBlock.x+populationGenerationBlockSize,currentBlock.y+populationGenerationBlockSize);
			Vector2 leftCorner=new Vector2(currentBlock.x+populationGenerationBlockSize,currentBlock.y);
			Vector2 rightCorner=new Vector2(currentBlock.x,currentBlock.y+populationGenerationBlockSize);
			if(inBoundsOfWorld(diagonalCorner)&&inBoundsOfWorld(leftCorner)&&inBoundsOfWorld(rightCorner)){
				if(populationDensity[(int)diagonalCorner.x,(int)diagonalCorner.y]==-1){
					populationDensity[(int)diagonalCorner.x,(int)diagonalCorner.y]=Random.Range(0,100);
				}
				if(populationDensity[(int)leftCorner.x,(int)leftCorner.y]==-1){
					populationDensity[(int)leftCorner.x,(int)leftCorner.y]=Random.Range(0,100);
				}
				if(populationDensity[(int)rightCorner.x,(int)rightCorner.y]==-1){
					populationDensity[(int)rightCorner.x,(int)rightCorner.y]=Random.Range(0,100);
				}
				for(int y=(int)currentBlock.x;y<=(int)leftCorner.x;y++){
					for(int x=(int)currentBlock.y;x<=(int)rightCorner.y;x++){
						if(populationDensity[y,x]==-1){
							double distToRightcorner=rightCorner.y-x;
							double distToCenterInx=x-currentBlock.y;

							double distToLeftcorner=leftCorner.x-y;
							double distToCenterIny=y-currentBlock.x;
							double range=populationGenerationBlockSize;
							double interpolatedValuex=(distToCenterInx/range)*populationDensity[(int)currentBlock.x,(int)currentBlock.y]+(distToRightcorner/range)*populationDensity[(int)rightCorner.x,(int)rightCorner.y];
							double interpolatedValuey=(distToCenterIny/range)*populationDensity[(int)currentBlock.x,(int)currentBlock.y]+(distToLeftcorner/range)*populationDensity[(int)leftCorner.x,(int)leftCorner.y];
							populationDensity[y,x]=(int)(interpolatedValuex*0.5+interpolatedValuey*0.5);
						}
					}
				}


				activeBlocks.Enqueue(leftCorner);
				
				activeBlocks.Enqueue(rightCorner);
				

			}


		}

		for (int y=0; y<worldHeight; y++) {
			for(int x=0;x<worldWidth;x++){
				if(populationDensity[y,x]==-1){
					populationDensity[y,x]=Random.Range (0,100);
				}
			}		
		}

		//////////place initial road starting point
		/*
		bool placedStartingPoint = false;
		while (!placedStartingPoint) {
			int y=Random.Range(0,worldHeight-1);
			int x=Random.Range(0,worldWidth-1);
			if(walkable[y,x]){
				gridForRoadPlacement[y,x]=true;
				placedStartingPoint=true;
			}
		}
		*/

		//debug
		/*
		Debug.Log ("PopulationDensity");

		for (int y=0; y<worldHeight; y++) {
			string row="";
			for(int x=0;x<worldWidth;x++){
				row+=populationDensity[y,x]+" ";
				
			}		
			Debug.Log (row);
		}

		Debug.Log ("Walkable");


		for (int y=0; y<worldHeight; y++) {
			string row="";
			for(int x=0;x<worldWidth;x++){
				row+=walkable[y,x]+" ";
				
			}		
			Debug.Log (row);
		}
		*/









	}


}
