import string
import cherrypy
import json
import numpy as np
import tensorflow as tf

class Network():
	def __init__(self,a):
		self.a = a
		self.model = tf.keras.Sequential([
							tf.keras.layers.Dense(128, activation='relu'),
							tf.keras.layers.Dense(10)
						])

		self.model.compile(optimizer='adam',
              loss=tf.keras.losses.SparseCategoricalCrossentropy(from_logits=True),
              metrics=['accuracy'])



	def fit(self, input, target):
		#output = model.fit(train_images, train_labels, epochs=10)
		return self.a

	def predict(self, input):
		return 'predict' + self.a

class CalculatorWebService(object):
	#Required to be accessable online
	exposed=True

	def __init__(self):
		self.net = Network('1.0')

	#@cherrypy.expose
	def FIT(self):
		msg = cherrypy.request.body.readline().decode("utf-8")
		print(msg)
		b = self.net.fit()
		output = b
		return output

	def PREDICT(self,*path,**query):
		msg = cherrypy.request.body.read()
		output = net.predict() + msg
		return output

	def GET(self,*path,**query):
		output = 'Get'
		return output

	def POST(self,*path,**query):
		msg = cherrypy.request.body.read()
		print(msg)
		output = 'Post'
		return output

	def PUT(self,*path,**query):
		msg = cherrypy.request.body.read().decode("utf-8") 
		msg = msg.split('.')
		result = {'action':msg[4], 'value':[0.35,0]}
		output = json.dumps(result)
		return output

	def DELETE(self,*path,**query):
		output = 'Get'
		return output

#'request.dispatch': cherrypy.dispatch.MethodDispatcher() => switch from default URL to HTTP compliant approch
conf = { '/': {	'request.dispatch': cherrypy.dispatch.MethodDispatcher(),
								'tools.sessions.on':True} 
					}
cherrypy.tree.mount(CalculatorWebService(), '/', conf)

cherrypy.config.update({'servet.socket_host':'0.0.0.0'})
cherrypy.config.update({'servet.socket_port':'8080'})

cherrypy.engine.start()
cherrypy.engine.block()
