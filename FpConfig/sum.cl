kernel void sum(
	global const float *a,
	global const float *b,
	global float *c)
{
	int gid = get_global_id(0);
	c[gid] = a[gid] + b[gid];
}
