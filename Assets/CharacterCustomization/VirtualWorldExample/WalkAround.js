function Start()
{
    // Rotate the character to a random direction.
    transform.Rotate(0, Random.Range(0, 360), 0);
}

function FixedUpdate()
{
    // Move the character a bit each frame.
    transform.position += transform.forward * .02;
    
    // Destroy the character when it's out of view.
    if (Vector3.Distance(Vector3.zero,transform.position) > 20)
		Destroy(gameObject);
}