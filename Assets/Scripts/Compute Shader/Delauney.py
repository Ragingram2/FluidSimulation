
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.tri
import matplotlib.collections
numSeeds = 24
radius = 100
seeds = radius * np.random.random((numSeeds, 2))
print("seeds:\n", seeds)
print("BBox Min:", np.amin(seeds, axis=0),"Bbox Max: ", np.amax(seeds, axis=0))
center = np.mean(seeds, axis=0)
print("Center:", center)
center = np.asarray(center)
# Create coordinates for the corners of the frame
coords = [center+radius*np.array((-1, -1)),center+radius*np.array((+1, -1)),center+radius*np.array((+1, +1)),center+radius*np.array((-1, +1))]

def circumcenter( tri):#finds the circumcenter
"""Compute circumcenter and circumradius of a triangle in 2D.
Uses an extension of the method described here:
http://www.ics.uci.edu/~eppstein/junkyard/circumcenter.html
"""
pts = np.asarray([coords[v] for v in tri])
   pts2 = np.dot(pts, pts.T)
   A = np.bmat([[2 * pts2, [[1],[1],[1]]],[[[1, 1, 1, 0]]]])
   b = np.hstack((np.sum(pts * pts, axis=1), [1]))
   x = np.linalg.solve(A, b)
   bary_coords = x[:-1]
   center = np.dot(bary_coords, pts)
   # radius = np.linalg.norm(pts[0] - center) # euclidean distance
   radius = np.sum(np.square(pts[0] - center))  # squared distance
   return (center, radius)
# Create two dicts to store triangle neighbours and circumcircles.
triangles = {}
circles = {}
# Create two CCW triangles for the frame
T1 = (0, 1, 3)
T2 = (2, 3, 1)
triangles[T1] = [T2, None, None]
triangles[T2] = [T1, None, None]


def inCircleFast( tri, p):
"""Check if point p is inside of precomputed circumcircle of tri.
"""
  center, radius = circles[tri]
  return np.sum(np.square(center - p)) <= radius
# Compute circumcenters and circumradius for each triangle
for t in triangles:
    circles[t] = circumcenter(t)


def addPoint(p):
"""Add a point to the current DT, and refine it using Bowyer-Watson.
"""
p = np.asarray(p)
    idx = len(coords)
    coords.append(p)
    # Search the triangle(s) whose circumcircle contains p
    bad_triangles = []
    for T in triangles:
         # Choose one method: inCircleRobust(T, p) or inCircleFast(T, p)
if inCircleFast(T, p):   
            bad_triangles.append(T)
       # Find the CCW boundary (star shape) of the bad triangles,
       # expressed as a list of edges (point pairs) and the opposite
       # triangle to each edge.
       boundary = []
       # Choose a "random" triangle and edge
       T = bad_triangles[0]
       edge = 0
       # get the opposite triangle of this edge
       while True:
          # Check if edge of triangle T is on the boundary...
          # if opposite triangle of this edge is external to the list    
          tri_op = triangles[T][edge]
          if tri_op not in bad_triangles:
            # Insert edge and external triangle into boundary list
boundary.append((T[(edge+1) % 3], T[(edge-1) % 3], tri_op)) 
             # Move to next CCW edge in this triangle
edge = (edge + 1) % 3
            # Check if boundary is a closed loop
            if boundary[0][0] == boundary[-1][1]:
               break
          else:
           # Move to next CCW edge in opposite triangle
           edge = (triangles[tri_op].index(T) + 1) % 3
           T = tri_op
     # Remove triangles too near of point p of our solution
       for T in bad_triangles:
         del triangles[T]
         del circles[T]
      #Retriangle the hole left by bad_triangles
      new_triangles = []
      for (e0, e1, tri_op) in boundary:
          # Create a new triangle using point p and edge extremes
          T = (idx, e0, e1)
          # Store circumcenter and circumradius of the triangle
         circles[T] = circumcenter(T)
         # Set opposite triangle of the edge as neighbour of T
         triangles[T] = [tri_op, None, None]
         # Try to set T as neighbour of the opposite triangle
         if tri_op:
            # search the neighbour of tri_op that use edge (e1, e0)
for i, neigh in enumerate(triangles[tri_op]):
if neigh:
                if e1 in neigh and e0 in neigh:
                     # change link to use our new triangle
triangles[tri_op][i] = T
# Add triangle to a temporal list
new_triangles.append(T)
   # Link the new triangles each another
   N = len(new_triangles)
   for i, T in enumerate(new_triangles):
      triangles[T][1] = new_triangles[(i+1) % N]   # next
triangles[T][2] = new_triangles[(i-1) % N]   # previous
# Insert all seeds one by one
for s in seeds: 
   addPoint(s)
# Create a plot with matplotlib.pyplot
fig, ax = plt.subplots()
ax.margins(0.1)
ax.set_aspect('equal')
plt.axis([-1, radius+1, -1, radius+1])
# Plot our Delaunay triangulation (plot in blue)
cx, cy = zip(*seeds)
dt_tris = [(a-4, b-4, c-4) for (a, b, c) in triangles if a > 3 and b > 3 and c > 3]
ax.triplot(matplotlib.tri.Triangulation(cx, cy, dt_tris), 'bo--')