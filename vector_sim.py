"""Simple vector AXPY simulation. Run with Python 3; no packages needed."""
import math
import csv

n, max_vl, startup = 1024, 64, 6
x = [i * 0.125 for i in range(n)]
y = [i * 0.25 for i in range(n)]
scalar_cycles = 5 * n  # Two loads, multiply, add, store; one cycle each.

with open('vector_results.csv', 'w', newline='') as file:
    writer = csv.writer(file)
    writer.writerow(['lanes', 'vector_cycles', 'scalar_cycles', 'speedup'])
    print(f'N={n}; maximum vector length={max_vl}; startup={startup}')
    print('Model: five serialized instructions per chunk; ideal memory')
    for lanes in [1, 4, 8]:
        z, cycles = [], 0
        for start in range(0, n, max_vl):
            vl = min(max_vl, n - start)
            z.extend(1.25 * x[i] + y[i] for i in range(start, start + vl))
            cycles += 5 * (startup + math.ceil(vl / lanes))
        assert z == [1.25 * x[i] + y[i] for i in range(n)]
        speedup = scalar_cycles / cycles
        writer.writerow([lanes, cycles, scalar_cycles, speedup])
        print(f'{lanes} lanes: {cycles} modeled cycles; speedup={speedup:.2f}')
    print('All correctness checks PASS. Modeled cycles are not hardware timings.')
