using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Grid : MonoBehaviour {
	//debug
	private bool onlyDisplayPath = false;

	//
	public Vector2 gridWorldSize=new Vector2(10,10);
	public float nodeRadius=0.5f;
	List<Node> currentPath;
	Node[,] grid;

	float nodeDiam;
	int gridSizeX, gridSizeY;


	void Start(){
		nodeDiam = nodeRadius * 2;
		gridSizeX = Mathf.RoundToInt (gridWorldSize.x / nodeDiam);
		gridSizeY = Mathf.RoundToInt (gridWorldSize.y / nodeDiam);


	}

	void Update(){

	}


	public List<Node> CreateGridAndFindPath(Vector3 target, List<Vector3> unwalkable, List<Vector3> pointVel){
		CreateGrid ();
		int pointVelCount = 0;
		foreach (Vector3 pos in unwalkable) {
			if(isInGrid (pos)){
				Node current=NodeFromWorldPoint(pos);

				current.walkable=false;
				foreach(Node n in GetNeighbours(current)){
					//n.walkable=false;
				}
				float powScale=1;
				float powProp=1;
				if(pointVel[pointVelCount]!=Vector3.zero){
					powScale=2;
					powProp=100;
				}else{
					//continue;
				}
				foreach(Node n in grid){
					if(n.distanceToCollisionObject<powProp/Mathf.Pow (Vector3.Distance(n.worldPosition,pos),powScale)){
						n.distanceToCollisionObject=powProp/Mathf.Pow (Vector3.Distance(n.worldPosition,pos),powScale);
					}

				}
			}
			pointVelCount++;

		}
		Node startingNode = NodeFromWorldPoint (transform.position);
		foreach (Node n in grid) {
			if(n.gridY<startingNode.gridY){
				n.walkable=false;
			}	
		}

		
		foreach(Node n in GetNeighbours(startingNode)){
			if(!(n.gridX==startingNode.gridX&&n.gridY==startingNode.gridY+1)){
				n.walkable=false;}

		}


		
		FindPath (transform.position, target);
		return currentPath;
	}

	private bool isInGrid(Vector3 worldPos){
		Vector3 relPos = worldPos - transform.position;
		float percentX = relPos.x / gridWorldSize.x + 0.5f;
		float percentY = relPos.z / gridWorldSize.y + 0.5f;
		//assuming point is in grid
		if (percentX < 0 || percentX > 1) {
			return false;		
		}
		if (percentY < 0 || percentY > 1) {
			return false;		
		}
		return true;
	}

	public Node NodeFromWorldPoint(Vector3 worldPos){
		Vector3 relPos = worldPos - transform.position;
		relPos = Quaternion.Inverse (transform.rotation) * relPos;
		float percentX = relPos.x / gridWorldSize.x + 0.5f;
		float percentY = relPos.z / gridWorldSize.y + 0.5f;
		//assuming point is in grid
		percentX = Mathf.Clamp01 (percentX);
		percentY = Mathf.Clamp01 (percentY);
		int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
		int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
		return grid [x, y];
	}
	void CreateGrid(){
		grid=new Node[gridSizeX,gridSizeY];
		Vector3 bottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
		for(int x=0;x<gridSizeX;x++){
			for(int y=0;y<gridSizeY;y++){
				Vector3 currentPos=bottomLeft+Vector3.right*(x*nodeDiam+nodeRadius)+Vector3.forward*(y*nodeDiam+nodeRadius);
				currentPos=currentPos-transform.position;
				currentPos=transform.rotation*currentPos;
				currentPos+=transform.position;
				//bool walkable=!(Physics.CheckSphere(currentPos,nodeRadius,unWalkableMask);
				grid[x,y]=new Node(true,currentPos,x,y);
			}
		}
	}

	void OnDrawGizmos(){
		Gizmos.DrawWireCube (transform.position, new Vector3 (gridWorldSize.x, 1, gridWorldSize.y));

		if (grid != null) {

						if (onlyDisplayPath) {

								if (currentPath != null) {
										Gizmos.color = Color.black;
										foreach (Node n in currentPath) {
												Gizmos.DrawCube (n.worldPosition, new Vector3 (nodeDiam - 0.1f, 0.5f, nodeDiam - 0.1f));
										}
								}

						} else {
				Debug.Log ("!!!!!!!!!!!!!------------------!!!!!!!!!!!!!!!!!!!!!!");
								foreach (Node n in grid) {
										Gizmos.color = (n.walkable) ? Color.white : Color.red;
										//Debug.Log (n.worldPosition+" "+transform.position);
										if (currentPath != null) {
												if (currentPath.Contains (n)) {

														Gizmos.color = Color.black;
												}
										}
										Gizmos.DrawCube (n.worldPosition, new Vector3 (nodeDiam - 0.1f, 0.5f, nodeDiam - 0.1f));
								}
						}
				}
	}

	public List<Node> GetNeighbours(Node node){
		List<Node> neighbours=new List<Node>();

		for (int x=-1; x<=1; x++) {
			for(int y=-1;y<=1;y++){
				if(x==0&&y==0){
					continue;
				}
				int checkX=node.gridX+x;
				int checkY=node.gridY+y;
				if(checkX>=0&&checkX<gridSizeX&&checkY>=0&&checkY<gridSizeY){
					neighbours.Add (grid[checkX,checkY]);
				}
			}		
		}
		return neighbours;
	}

	double GetDistance(Node nodeA, Node nodeB){
		//for Euclidian
		int distX = Mathf.Abs (nodeA.gridX - nodeB.gridX);
		int distY = Mathf.Abs (nodeA.gridY - nodeB.gridY);

		if (distX > distY) {
			return 14 * distY + 10 * (distX - distY);		
		} else {
			return 14 * distX + 10 * (distY - distX);
		}
	}

	double HeuristicDistance(Node nodeA, Node nodeB){
		return 0;
	}

	double GetMaxAvoidDistance(Node nodeA, Node nodeB){
		return nodeB.distanceToCollisionObject;
		if (nodeB.distanceToCollisionObject < nodeA.distanceToCollisionObject) {
			return 0;		
		} else {
			return nodeB.distanceToCollisionObject - nodeA.distanceToCollisionObject;
		}
	}


	public int MaxSize{
		get{
			return gridSizeX*gridSizeY;
		}
	}

	public void FindPath(Vector3 startPos, Vector3 targetPos){
		//Debug.Log (MaxSize);


		Node startNode = NodeFromWorldPoint (startPos);
		Node targetNode = NodeFromWorldPoint (targetPos);

		bool canGetToTarget = false;
		Heap<Node> openSet = new Heap<Node> (MaxSize);
		HashSet<Node> closedSet = new HashSet<Node> ();
		openSet.Add (startNode);
		while (openSet.Count>0) {

			Node currentNode=openSet.RemoveFirst();

			closedSet.Add (currentNode);

			if(currentNode==targetNode){
				canGetToTarget=true;
				//Debug.Log (targetNode.fCost);
				break;
			}

			foreach(Node neighbour in GetNeighbours(currentNode)){
				if(!neighbour.walkable||closedSet.Contains (neighbour)){
					continue;
				}
				double newMovementCostToNeighbour=currentNode.gCost+GetMaxAvoidDistance(currentNode,neighbour);
				if(newMovementCostToNeighbour<neighbour.gCost||!openSet.Contains(neighbour)){
					neighbour.gCost=newMovementCostToNeighbour;
					neighbour.hCost=HeuristicDistance(neighbour,targetNode);
					neighbour.parent=currentNode;
					if(!openSet.Contains (neighbour)){
						openSet.Add (neighbour);
					}else{
						openSet.UpdateItem(neighbour);
					}
				}
			}



		}


		if (canGetToTarget) {
			currentPath = RetracePath (startNode, targetNode);
		} else {
			currentPath=new List<Node>();
		}

	}

	List<Node> RetracePath(Node start,Node end){
		List<Node> path = new List<Node> ();
		Node current = end;
		while (current!=start) {
			path.Add(current);
			current=current.parent;
		}
		path.Reverse ();

		return path;


	}

}
