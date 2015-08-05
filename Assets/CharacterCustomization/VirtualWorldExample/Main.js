var prefab : GameObject;

// This script creates a character prefab every 3 seconds,
// and places it at a random position.
while(true)
{
	go = Instantiate(prefab);
	go.transform.position = new Vector3(Random.value * 10, 0, Random.value * 10);
	yield new WaitForSeconds(3);
}
