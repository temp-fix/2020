#pragma strict



public class DataPoint {
	var relativeVelocity; var relativePosition;
	var index;
	var sensorIndex;

	function DataPoint(relVel,relPos,index_){
		relativeVelocity = relVel;
		relativePosition = relPos;
		index = index_;
		
	}

}
