kernel void sum(
	constant float *restrict a,
	constant float *restrict b,
	global float *restrict c)
{
	int gid = get_global_id(0);
	c[gid] = a[gid] + b[gid];
}
