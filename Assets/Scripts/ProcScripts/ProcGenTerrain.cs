using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProcGenTerrain : MonoBehaviour {

	void Start(){
		GenerateTerrain (gameTerrain);
		generateStreetMap (100,100);
		initSpawn ();
		createViewBlockers ();
		for (int i=0; i<1; i++) {
			spawnNPAI();		
		}
		Debug.Log ("Done");
	}

	//NPAI spawner

	public GameObject NPAIGameObject;
	List<IntPair> possibleSpawningPositions;
	int[,] seenForPathPlacement;
	int currentRoadPlacementCycle=0;
	void initSpawn(){
		possibleSpawningPositions = new List<IntPair> ();
		seenForPathPlacement = new int[streetMapHeight, streetMapWidth];
		for (int i=0; i<streetMapHeight; i++) {
			for(int e=0;e<streetMapWidth;e++){
				if(gridForRoadPlacement[i,e]){
					possibleSpawningPositions.Add (new IntPair(i,e));
					seenForPathPlacement[i,e]=-1;
				}
			}		
		}
		currentRoadPlacementCycle = 0;



	}

	IntPair getNextPath(IntPair current, int cycleNumber){
		//Debug.Log ("!");
		List<IntPair> possibleDir = new List<IntPair> ();
		for (int i=-1; i<=1; i++) {
			for(int e=-1;e<=1;e++){
				int nxtY=current.first+i;
				int nxtX=current.second+e;
				if(inBoundsOfWorld(nxtY,nxtX)){
					if(seenForPathPlacement[nxtY,nxtX]!=cycleNumber){
						//Debug.Log (seenForPathPlacement[nxtY,nxtX]);
						possibleDir.Add (new IntPair(nxtY,nxtX));
					}
				}
			}
		}
		if (possibleDir.Count == 0) {
			return new IntPair(-1,-1);		
		}
		int ind = Random.Range (0, possibleDir.Count);
		return possibleDir [ind];
	}

	void spawnNPAI(){
		++currentRoadPlacementCycle;

		int startPos = Random.Range (0, possibleSpawningPositions.Count);
		IntPair currentPos = possibleSpawningPositions [startPos];
		List<Vector3> path = new List<Vector3> ();
		path.Add (new Vector3 (currentPos.second, 0, currentPos.first));
		IntPair nxt = getNextPath (currentPos, currentRoadPlacementCycle);
		seenForPathPlacement [currentPos.first, currentPos.second] = currentRoadPlacementCycle;
		int pLength = 1;
		int maxLen = Random.Range (50, 200);
		while (nxt.first!=-1&&pLength<maxLen) {

			path.Add (new Vector3(nxt.second,0,nxt.first));
			//Debug.Log (nxt.first+" "+nxt.second+" "+currentRoadPlacementCycle);

			seenForPathPlacement [nxt.first, nxt.second] = currentRoadPlacementCycle;
			currentPos.first=nxt.first;
			currentPos.second=nxt.second;
			++pLength;
			nxt=getNextPath (currentPos, currentRoadPlacementCycle);
		}

		Vector3[] arrPath = new Vector3[path.Count];
		float stepSizeY=gameTerrain.terrainData.heightmapHeight/streetMapHeight;
		float stepSizeX=gameTerrain.terrainData.heightmapWidth/streetMapWidth;
		for (int i=0; i<path.Count; i++) {
			arrPath[i]=new Vector3(path[i].x*stepSizeX,0,path[i].z*stepSizeY);
			//Debug.Log (arrPath[i]);
		}

		//return;
		GameObject newSpawnedCar = (GameObject)Instantiate (NPAIGameObject, new Vector3 (path [0].x, 100, path [0].z), Quaternion.identity);
		NPAIForProcGen npaiScript = (NPAIForProcGen)newSpawnedCar.GetComponent ("NPAIForProcGen");
		npaiScript.checkPoints = arrPath;
		npaiScript.startCar ();


	
	}




	//Street map generation

	void generateStreetMap(int Width, int Heigt){
		streetMapHeight = Width;
		streetMapHeight = streetMapHeight;
		Random.seed = 0;
			initGridsForGeneration (Width, Heigt);
		createRoadNetwork ();


	}

	bool[,] gridForRoadPlacement;
	int[,] populationDensity,numberOfIntersections,currentPathLength;
	Vector2[,] roadDirection;
	bool[,] walkable;
	int streetMapHeight,streetMapWidth;
	public GameObject viewBlocker;
	bool noRoadsNear(int h, int w){
		for(int i=-1;i<=1;i++){
			for(int e=-1;e<=1;e++){
				int nw=w+e;
				int nh=h+i;
				if(inBoundsOfWorld(nh,nw)){
					if(gridForRoadPlacement[nh,nw]){
						return false;
					}
				}
			}
		}
		return true;
	}

	void placeViewBlocker(int h, int w){
		float blockerWidth = gameTerrain.terrainData.heightmapWidth / streetMapWidth;
		float blockerHeight=gameTerrain.terrainData.heightmapHeight / streetMapHeight;
		float posx = w; 
		float posz = h; 
		float stepSizeY=4*gameTerrain.terrainData.heightmapHeight/streetMapHeight;
		float stepSizeX=4*gameTerrain.terrainData.heightmapWidth/streetMapWidth;
		GameObject blocker = (GameObject)Instantiate (viewBlocker, new Vector3 (posx*stepSizeX- blockerWidth / 2, 0, posz*stepSizeY- blockerHeight / 2), Quaternion.identity);
		blocker.transform.localScale = new Vector3 (blockerWidth, 100f, blockerHeight);
	}

	void createViewBlockers(){
		for(int h=0;h<streetMapHeight;h++){
			for(int w=0;w<streetMapWidth;w++){
				if(noRoadsNear(h,w)){
					placeViewBlocker(h,w);
				}
			}
		}

	}
	void generateRoadNetwork(){
		bool[,] seen = new bool[streetMapHeight, streetMapWidth];
		for(int i=0;i<streetMapHeight;i++){
			for(int e=0;e<streetMapWidth;e++){
				seen[i,e]=false;
			}
		}
		Queue<Vector4> activePoints = new Queue<Vector4> ();
		activePoints.Enqueue (new Vector4 (streetMapHeight / 2, streetMapWidth / 2,-1,-1));
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
		}
		
	}

	void createRoadNetwork(){
		Queue<Vector3> activePoints = new Queue<Vector3> ();//y,x,dist since last branched
		
		int starty = streetMapHeight/2, startx = streetMapWidth/2;
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
		
		
		return new Vector2 (y, x);
	}

	bool inBoundsOfWorld(int y, int x){
		if (0 <= y && y < streetMapHeight) {
			if(0<=x && x<streetMapWidth){
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


	void initGridsForGeneration(int heightOfWorld, int widthOfWorld){
		streetMapHeight = heightOfWorld;
		streetMapWidth = widthOfWorld;
		gridForRoadPlacement=new bool[heightOfWorld,widthOfWorld];
		walkable=new bool[heightOfWorld,widthOfWorld];
		populationDensity=new int[heightOfWorld,widthOfWorld];
		numberOfIntersections=new int[heightOfWorld,widthOfWorld];
		currentPathLength = new int[heightOfWorld, widthOfWorld];
		roadDirection = new Vector2[heightOfWorld, widthOfWorld];

		////init gridForRoadPlacement, walkable, numberOfIntersections
		for (int y=0; y<streetMapHeight; y++) {
			for(int x=0;x<streetMapWidth;x++){
				gridForRoadPlacement[y,x]=false;
				walkable[y,x]=true;
				numberOfIntersections[y,x]=0;
				populationDensity[y,x]=-1;
				currentPathLength[y,x]=0;
				roadDirection[y,x]=new Vector2(0,0);
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



	//Terrain Generation


	public Terrain gameTerrain;
	public void GenerateTerrain(Terrain t)
	{
		float tileSize= 3;
		
		
		float divRange = 7;
		//float divRange = 2f;
		//float tileSize= 10;
		
		//Heights For Our Hills/Mountains
		float[,] hts = new float[t.terrainData.heightmapWidth, t.terrainData.heightmapHeight];
		for (int i = 0; i < t.terrainData.heightmapWidth; i++)
		{
			for (int k = 0; k < t.terrainData.heightmapHeight; k++)
			{
				hts[i, k] = Mathf.PerlinNoise(((float)i / (float)t.terrainData.heightmapWidth) * tileSize, ((float)k / (float)t.terrainData.heightmapHeight) * tileSize)/ divRange;
			}
		}

		t.terrainData.SetHeights(0, 0, hts);
	}
}