kernel void reduction(
	global const float4 *restrict data,
	local float4 *restrict partialSums,
	global float4 *restrict workGroupResults)
{
	const int globalId = get_global_id(0);
	const int localId = get_local_id(0);
	const int workGroupId = get_group_id(0);
	const int workGroupSize = get_local_size(0);

	partialSums[localId] = data[globalId];

	barrier(CLK_LOCAL_MEM_FENCE);

	for (int i = workGroupSize >> 1; i > 0; i >>= 1)
	{
		if (localId < i)
		{
			partialSums[localId] += partialSums[localId + i];
		}

		barrier(CLK_LOCAL_MEM_FENCE);
	}

	if (localId == 0)
	{
		workGroupResults[workGroupId] = partialSums[0];
	}
}
